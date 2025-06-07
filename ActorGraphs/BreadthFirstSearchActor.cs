using System.Collections.Frozen;
using System.Numerics;
using Actors;
using Graphs;
using Microsoft.Extensions.Logging;

namespace ActorGraphs;

public sealed class BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight> : ActorBase<BreadthFirstSearchActorId, BreadthFirstSearchMessage>
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TEdge : struct, IDirectedEdge<TNode, TValue, TWeight, TEdge>
    where TValue : struct, IEquatable<TValue>
    where TWeight : struct, IComparable<TWeight>, IAdditionOperators<TWeight, TWeight, TWeight>, IAdditiveIdentity<TWeight, TWeight>
{
    public BreadthFirstSearchRunner<TNode, TValue, TEdge, TWeight> Runner { get; }
    public TNode Node { get; }
    public FrozenDictionary<BreadthFirstSearchActorId, NeighbourInfo<TNode, TValue, TEdge, TWeight>> Neighbours { get; private set; }
    public TWeight? TotalWeightFromStart { get; private set; }

    internal BreadthFirstSearchActor(BreadthFirstSearchActorId id, BreadthFirstSearchRunner<TNode, TValue, TEdge, TWeight> runner, TNode node, ILogger logger) : base(id, logger)
    {
        Runner = runner;
        Node = node;
        Neighbours = FrozenDictionary<BreadthFirstSearchActorId, NeighbourInfo<TNode, TValue, TEdge, TWeight>>.Empty;
        TotalWeightFromStart = null; // No weight means "unreachable"
    }

    internal void InitialiseNeighbours(FrozenDictionary<BreadthFirstSearchActorId, NeighbourInfo<TNode, TValue, TEdge, TWeight>> neighbours)
    {
        Neighbours = neighbours;
    }

    protected override Task HandleMessageAsync(BreadthFirstSearchMessage message)
        => message switch
        {
            BreadthFirstSearchMessage.StartBreadthFirstSearchMessage<TValue> startMessage => HandleStartMessageAsync(startMessage),
            BreadthFirstSearchMessage.UpdateWeightMessage<TValue, TWeight> updateWeightMessage => HandleUpdateWeightMessageAsync(updateWeightMessage),
            BreadthFirstSearchMessage.GetTotalWeightFromStartMessage getTotalWeightFromStartMessage => HandleGetTotalWeightFromStartMessageAsync(getTotalWeightFromStartMessage),
            _ => Task.CompletedTask
        };

    protected override ValueTask DisposeActorAsync()
    {
        // Task[] disposeNeighbourTasks = Neighbours
        //     .Values
        //     .Select(info => info.Neighbour.DisposeAsync().AsTask())
        //     .ToArray();
        // await Task.WhenAll(disposeNeighbourTasks);
        return ValueTask.CompletedTask; // No-op because Runner will clean up
    }

    private async Task HandleStartMessageAsync(BreadthFirstSearchMessage.StartBreadthFirstSearchMessage<TValue> startMessage)
    {
        // Check if message comes from the runner this actor knows
        if (startMessage.SenderId is not BreadthFirstSearchRunnerId runnerId) return;
        if (!runnerId.Equals(Runner.Id)) return;

        // Expect start node value to be equal to own node value
        if (!Node.Value.Equals(startMessage.StartValue)) return;

        // If no neighbours, signal to runner that work is finished
        if (Neighbours.Count == 0)
        {
            BreadthFirstSearchRunnerMessage.NoNeighboursMessage<TValue> noNeighboursMessage =
                BreadthFirstSearchRunnerMessage.NoNeighbours(Id, Node.Value);
            await Runner.SendAsync(noNeighboursMessage);

            // Leave early before doing any work by accident
            return;
        }

        // Initialize own weight to "zero"
        TotalWeightFromStart = TWeight.AdditiveIdentity;

        // Create update weight message
        BreadthFirstSearchMessage.UpdateWeightMessage<TValue, TWeight> updateWeightMessage =
            BreadthFirstSearchMessage.UpdateTotalWeight(Id, startMessage.StartValue, TotalWeightFromStart!.Value);

        Guid taskId = Guid.NewGuid();

        // Notify the runner that work has started
        await Runner.SendAsync(BreadthFirstSearchRunnerMessage.WorkStarted(Id, taskId));

        // Message neighbours (do work)
        await NotifyNeighbours(updateWeightMessage);

        // Notify the runner that work has finished
        await Runner.SendAsync(BreadthFirstSearchRunnerMessage.WorkFinished(Id, taskId));
    }

    private async Task HandleUpdateWeightMessageAsync(BreadthFirstSearchMessage.UpdateWeightMessage<TValue, TWeight> updateWeightMessage)
    {
        // Check if message comes from a neighbour
        if (updateWeightMessage.SenderId is not BreadthFirstSearchActorId neighbourId) return;
        if (!Neighbours.TryGetValue(neighbourId, out NeighbourInfo<TNode, TValue, TEdge, TWeight> neighbourInfo)) return;

        // Calculate total weight from signalled path
        TWeight signalledTotal = updateWeightMessage.TotalWeightFromStart + neighbourInfo.DistanceFromNeighbour;

        // Compare to own total weight
        bool isSmallerTotal = TotalWeightFromStart is null || signalledTotal.CompareTo(TotalWeightFromStart.Value) < 0;

        // If new weight is less than own total weight, update own weight and signal to neighbours
        if (isSmallerTotal)
        {
            TotalWeightFromStart = signalledTotal;
            BreadthFirstSearchMessage.UpdateWeightMessage<TValue, TWeight> message =
                BreadthFirstSearchMessage.UpdateTotalWeight(Id, updateWeightMessage.StartValue, TotalWeightFromStart.Value); // Total weight is never null here

            // Create a task ID for the messaging work
            Guid taskId = Guid.NewGuid();

            // Notify the runner that work has started
            await Runner.SendAsync(BreadthFirstSearchRunnerMessage.WorkStarted(Id, taskId));

            // Message neighbours (do work)
            await NotifyNeighbours(message);

            // Notify the runner that work has finished
            await Runner.SendAsync(BreadthFirstSearchRunnerMessage.WorkFinished(Id, taskId));
        }
    }

    private async Task HandleGetTotalWeightFromStartMessageAsync(BreadthFirstSearchMessage.GetTotalWeightFromStartMessage getTotalWeightFromStartMessage)
    {
        // Check if message comes from the runner this actor knows
        if (getTotalWeightFromStartMessage.SenderId is not BreadthFirstSearchRunnerId runnerId) return;
        if (!runnerId.Equals(Runner.Id)) return;

        // Create and send message to signal total weight
        BreadthFirstSearchRunnerMessage.TotalWeightFromStartMessage<TWeight> sendTotalWeightMessage =
            BreadthFirstSearchRunnerMessage.SendTotalWeight(Id, TotalWeightFromStart);
        await Runner.SendAsync(sendTotalWeightMessage);
    }

    private Task NotifyNeighbours(BreadthFirstSearchMessage message)
    {
        foreach (var (neighbour, _) in Neighbours.Values)
        {
            _ = neighbour.SendAsync(message);
        }
        return Task.CompletedTask;
    }
}
