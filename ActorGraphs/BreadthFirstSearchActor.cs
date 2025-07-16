using System.Collections.Frozen;
using System.Numerics;
using Actors;
using Graphs;
using Microsoft.Extensions.Logging;

namespace ActorGraphs;

public sealed class BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight> : ActorBase<BreadthFirstSearchActorId, BreadthFirstSearchMessage, BreadthFirstSearchActorRef<TNode, TValue>>
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TEdge : struct, IDirectedEdge<TNode, TValue, TWeight, TEdge>
    where TValue : struct, IEquatable<TValue>
    where TWeight : struct, IComparable<TWeight>, IAdditionOperators<TWeight, TWeight, TWeight>, IAdditiveIdentity<TWeight, TWeight>
{
    public BreadthFirstSearchRunnerActorRef Runner { get; }
    public TNode Node { get; }
    public FrozenDictionary<BreadthFirstSearchActorRef<TNode, TValue>, TWeight> NeighbourDistances { get; private set; }
    public TWeight? TotalWeightFromStart { get; private set; }

    internal BreadthFirstSearchActor(BreadthFirstSearchActorId id, BreadthFirstSearchRunnerActorRef runner, TNode node, ILogger logger)
        : base(new BreadthFirstSearchActorRefFactory<TNode, TValue>(id, node), logger)
    {
        Runner = runner;
        Node = node;
        NeighbourDistances = FrozenDictionary<BreadthFirstSearchActorRef<TNode, TValue>, TWeight>.Empty;
        TotalWeightFromStart = null; // No weight means "unreachable"
    }

    internal void InitialiseNeighbours(FrozenDictionary<BreadthFirstSearchActorRef<TNode, TValue>, TWeight> neighbours)
    {
        NeighbourDistances = neighbours;
    }

    protected override Task HandleMessageAsync(BreadthFirstSearchMessage message)
        => message switch
        {
            BreadthFirstSearchMessage.StartBreadthFirstSearchMessage<TValue> startMessage => HandleStartMessageAsync(startMessage),
            BreadthFirstSearchMessage.UpdateWeightMessage<TValue, TWeight> updateWeightMessage => HandleUpdateWeightMessageAsync(updateWeightMessage),
            BreadthFirstSearchMessage.GetTotalWeightFromStartMessage getTotalWeightFromStartMessage => HandleGetTotalWeightFromStartMessageAsync(getTotalWeightFromStartMessage),
            BreadthFirstSearchMessage.SearchKickedOffMessage searchKickedOffMessage => HandleSearchKickedOffMessage(searchKickedOffMessage),
            _ => Task.CompletedTask
        };

    protected override ValueTask DisposeActorAsync()
        => ValueTask.CompletedTask; // No-op because Runner will clean up

    private async Task HandleStartMessageAsync(BreadthFirstSearchMessage.StartBreadthFirstSearchMessage<TValue> startMessage)
    {
        // Check if message comes from the runner this actor knows
        if (startMessage.SenderRef is not BreadthFirstSearchRunnerActorRef runnerRef) return;
        if (!runnerRef.Equals(Runner)) return;

        // Expect start node value to be equal to own node value
        if (!Node.Value.Equals(startMessage.StartValue)) return;

        // If no neighbours, signal to runner that work is finished
        if (NeighbourDistances.Count == 0)
        {
            BreadthFirstSearchRunnerMessage.NoNeighboursMessage<TValue> noNeighboursMessage =
                BreadthFirstSearchRunnerMessage.NoNeighbours(Reference, Node.Value);
            await Runner.SendAsync(noNeighboursMessage);

            // Leave early before doing any work by accident
            return;
        }

        // Initialize own weight to "zero"
        TotalWeightFromStart = TWeight.AdditiveIdentity;

        // Create update weight message
        BreadthFirstSearchMessage.UpdateWeightMessage<TValue, TWeight> updateWeightMessage =
            BreadthFirstSearchMessage.UpdateTotalWeight(Reference, startMessage.StartValue, TotalWeightFromStart!.Value);

        // Notify the runner that work has started
        await Runner.SendAsync(BreadthFirstSearchRunnerMessage.WorkStarted(Reference, startMessage.KickOffId));

        // Message neighbours (do work)
        await NotifyNeighbours(updateWeightMessage);
    }

    private async Task HandleUpdateWeightMessageAsync(BreadthFirstSearchMessage.UpdateWeightMessage<TValue, TWeight> updateWeightMessage)
    {
        // Check if message comes from a neighbour
        if (updateWeightMessage.SenderRef is not BreadthFirstSearchActorRef<TNode, TValue> neighbourRef) return;
        if (!NeighbourDistances.TryGetValue(neighbourRef, out TWeight distanceFromNeighbour)) return;

        // Calculate total weight from signalled path
        TWeight signalledTotal = updateWeightMessage.TotalWeightFromStart + distanceFromNeighbour;

        // Compare to own total weight
        bool isSmallerTotal = TotalWeightFromStart is null || signalledTotal.CompareTo(TotalWeightFromStart.Value) < 0;

        // If new weight is less than own total weight, update own weight and signal to neighbours
        if (isSmallerTotal)
        {
            TotalWeightFromStart = signalledTotal;
            BreadthFirstSearchMessage.UpdateWeightMessage<TValue, TWeight> message =
                BreadthFirstSearchMessage.UpdateTotalWeight(Reference, updateWeightMessage.StartValue, TotalWeightFromStart.Value); // Total weight is never null here

            // Create a task ID for the messaging work
            Guid taskId = Guid.NewGuid();

            // Notify the runner that work has started
            await Runner.SendAsync(BreadthFirstSearchRunnerMessage.WorkStarted(Reference, taskId));

            // Message neighbours (do work)
            await NotifyNeighbours(message);

            // Notify the runner that work has finished
            await Runner.SendAsync(BreadthFirstSearchRunnerMessage.WorkFinished(Reference, taskId));
        }
    }

    private async Task HandleGetTotalWeightFromStartMessageAsync(BreadthFirstSearchMessage.GetTotalWeightFromStartMessage getTotalWeightFromStartMessage)
    {
        // Check if message comes from the runner this actor knows
        if (getTotalWeightFromStartMessage.SenderRef is not BreadthFirstSearchRunnerActorRef runnerRef) return;
        if (!runnerRef.Equals(Runner)) return;

        // Create and send message to signal total weight
        BreadthFirstSearchRunnerMessage.TotalWeightFromStartMessage<TWeight> sendTotalWeightMessage =
            BreadthFirstSearchRunnerMessage.SendTotalWeight(Reference, TotalWeightFromStart);
        await Runner.SendAsync(sendTotalWeightMessage);
    }

    private async Task HandleSearchKickedOffMessage(BreadthFirstSearchMessage.SearchKickedOffMessage searchKickedOffMessage)
    {
        if (searchKickedOffMessage.SenderRef is not BreadthFirstSearchRunnerActorRef runnerRef) return;
        if (!runnerRef.Equals(Runner)) return;

        await Runner.SendAsync(BreadthFirstSearchRunnerMessage.WorkFinished(Reference, searchKickedOffMessage.KickOffId));
    }

    private async Task NotifyNeighbours(BreadthFirstSearchMessage message)
    {
        foreach (BreadthFirstSearchActorRef<TNode, TValue> neighbour in NeighbourDistances.Keys)
        {
            await neighbour.SendAsync(message);
        }
    }
}
