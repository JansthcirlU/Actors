using System.Collections.Frozen;
using System.Numerics;
using Actors;
using Graphs;
using Microsoft.Extensions.Logging;

namespace ActorGraphs.OrchestratedSearch;

public sealed class OrchestratedSearchActor<TNode, TValue, TEdge, TWeight> : ActorBase<OrchestratedSearchActorId, OrchestratedSearchMessage, OrchestratedSearchActorRef<TNode, TValue>>
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TEdge : struct, IDirectedEdge<TNode, TValue, TWeight, TEdge>
    where TValue : struct, IEquatable<TValue>
    where TWeight : struct, IComparable<TWeight>, IAdditionOperators<TWeight, TWeight, TWeight>, IAdditiveIdentity<TWeight, TWeight>
{
    public OrchestratedSearchRunnerActorRef Runner { get; }
    public TNode Node { get; }
    public FrozenDictionary<OrchestratedSearchActorRef<TNode, TValue>, TWeight> NeighbourDistances { get; private set; }
    public TWeight? TotalWeightFromStart { get; private set; }

    internal OrchestratedSearchActor(OrchestratedSearchActorId id, OrchestratedSearchRunnerActorRef runner, TNode node, ILogger logger)
        : base(new OrchestratedSearchActorRefFactory<TNode, TValue>(id, node), logger)
    {
        Runner = runner;
        Node = node;
        NeighbourDistances = FrozenDictionary<OrchestratedSearchActorRef<TNode, TValue>, TWeight>.Empty;
        TotalWeightFromStart = null; // No weight means "unreachable"
    }

    internal void InitialiseNeighbours(FrozenDictionary<OrchestratedSearchActorRef<TNode, TValue>, TWeight> neighbours)
    {
        NeighbourDistances = neighbours;
    }

    protected override Task HandleMessageAsync(OrchestratedSearchMessage message)
        => message switch
        {
            OrchestratedSearchMessage.StartOrchestratedSearchMessage<TValue> startMessage => HandleStartMessageAsync(startMessage),
            OrchestratedSearchMessage.UpdateWeightMessage<TValue, TWeight> updateWeightMessage => HandleUpdateWeightMessageAsync(updateWeightMessage),
            OrchestratedSearchMessage.GetTotalWeightFromStartMessage getTotalWeightFromStartMessage => HandleGetTotalWeightFromStartMessageAsync(getTotalWeightFromStartMessage),
            OrchestratedSearchMessage.SearchKickedOffMessage searchKickedOffMessage => HandleSearchKickedOffMessage(searchKickedOffMessage),
            _ => Task.CompletedTask
        };

    protected override ValueTask DisposeActorAsync()
        => ValueTask.CompletedTask; // No-op because Runner will clean up

    private async Task HandleStartMessageAsync(OrchestratedSearchMessage.StartOrchestratedSearchMessage<TValue> startMessage)
    {
        // Check if message comes from the runner this actor knows
        if (startMessage.SenderRef is not OrchestratedSearchRunnerActorRef runnerRef) return;
        if (!runnerRef.Equals(Runner)) return;

        // Expect start node value to be equal to own node value
        if (!Node.Value.Equals(startMessage.StartValue)) return;

        // If no neighbours, signal to runner that work is finished
        if (NeighbourDistances.Count == 0)
        {
            OrchestratedSearchRunnerMessage.NoNeighboursMessage<TValue> noNeighboursMessage =
                OrchestratedSearchRunnerMessage.NoNeighbours(Reference, Node.Value);
            await Runner.SendAsync(noNeighboursMessage);

            // Leave early before doing any work by accident
            return;
        }

        // Initialize own weight to "zero"
        TotalWeightFromStart = TWeight.AdditiveIdentity;

        // Create update weight message
        OrchestratedSearchMessage.UpdateWeightMessage<TValue, TWeight> updateWeightMessage =
            OrchestratedSearchMessage.UpdateTotalWeight(Reference, startMessage.StartValue, TotalWeightFromStart!.Value);

        // Notify the runner that work has started
        await Runner.SendAsync(OrchestratedSearchRunnerMessage.WorkStarted(Reference, startMessage.KickOffId));

        // Message neighbours (do work)
        await NotifyNeighbours(updateWeightMessage);
    }

    private async Task HandleUpdateWeightMessageAsync(OrchestratedSearchMessage.UpdateWeightMessage<TValue, TWeight> updateWeightMessage)
    {
        // Check if message comes from a neighbour
        if (updateWeightMessage.SenderRef is not OrchestratedSearchActorRef<TNode, TValue> neighbourRef) return;
        if (!NeighbourDistances.TryGetValue(neighbourRef, out TWeight distanceFromNeighbour)) return;

        // Calculate total weight from signalled path
        TWeight signalledTotal = updateWeightMessage.TotalWeightFromStart + distanceFromNeighbour;

        // Compare to own total weight
        bool isSmallerTotal = TotalWeightFromStart is null || signalledTotal.CompareTo(TotalWeightFromStart.Value) < 0;

        // If new weight is less than own total weight, update own weight and signal to neighbours
        if (isSmallerTotal)
        {
            TotalWeightFromStart = signalledTotal;
            OrchestratedSearchMessage.UpdateWeightMessage<TValue, TWeight> message =
                OrchestratedSearchMessage.UpdateTotalWeight(Reference, updateWeightMessage.StartValue, TotalWeightFromStart.Value); // Total weight is never null here

            // Create a task ID for the messaging work
            Guid taskId = Guid.NewGuid();

            // Notify the runner that work has started
            await Runner.SendAsync(OrchestratedSearchRunnerMessage.WorkStarted(Reference, taskId));

            // Message neighbours (do work)
            await NotifyNeighbours(message);

            // Notify the runner that work has finished
            await Runner.SendAsync(OrchestratedSearchRunnerMessage.WorkFinished(Reference, taskId));
        }
    }

    private async Task HandleGetTotalWeightFromStartMessageAsync(OrchestratedSearchMessage.GetTotalWeightFromStartMessage getTotalWeightFromStartMessage)
    {
        // Check if message comes from the runner this actor knows
        if (getTotalWeightFromStartMessage.SenderRef is not OrchestratedSearchRunnerActorRef runnerRef) return;
        if (!runnerRef.Equals(Runner)) return;

        // Create and send message to signal total weight
        OrchestratedSearchRunnerMessage.TotalWeightFromStartMessage<TWeight> sendTotalWeightMessage =
            OrchestratedSearchRunnerMessage.SendTotalWeight(Reference, TotalWeightFromStart);
        await Runner.SendAsync(sendTotalWeightMessage);
    }

    private async Task HandleSearchKickedOffMessage(OrchestratedSearchMessage.SearchKickedOffMessage searchKickedOffMessage)
    {
        if (searchKickedOffMessage.SenderRef is not OrchestratedSearchRunnerActorRef runnerRef) return;
        if (!runnerRef.Equals(Runner)) return;

        await Runner.SendAsync(OrchestratedSearchRunnerMessage.WorkFinished(Reference, searchKickedOffMessage.KickOffId));
    }

    private async Task NotifyNeighbours(OrchestratedSearchMessage message)
    {
        foreach (OrchestratedSearchActorRef<TNode, TValue> neighbour in NeighbourDistances.Keys)
        {
            await neighbour.SendAsync(message);
        }
    }
}
