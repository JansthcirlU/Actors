using System.Numerics;
using Actors;

namespace ActorGraphs;

public abstract record BreadthFirstSearchRunnerMessage(IActorId SenderId) : IMessageType<BreadthFirstSearchRunnerId>
{
    public sealed record TotalWeightFromStartMessage<TWeight>(IActorId SenderId, TWeight? TotalWeight) : BreadthFirstSearchRunnerMessage(SenderId)
        where TWeight : struct, IComparable<TWeight>, IAdditionOperators<TWeight, TWeight, TWeight>, IAdditiveIdentity<TWeight, TWeight>;
    public sealed record StartedWorkMessage(IActorId SenderId, Guid TaskId) : BreadthFirstSearchRunnerMessage(SenderId);
    public sealed record FinishedWorkMessage(IActorId SenderId, Guid TaskId) : BreadthFirstSearchRunnerMessage(SenderId);
    public sealed record NoNeighboursMessage<TValue>(IActorId SenderId, TValue ActorNodeValue) : BreadthFirstSearchRunnerMessage(SenderId);
    public sealed record RunFinishedMessage(IActorId SenderId) : BreadthFirstSearchRunnerMessage(SenderId);
    public sealed record RunFinishedImmediatelyMessage<TValue>(IActorId SenderId, TValue StartValue) : BreadthFirstSearchRunnerMessage(SenderId);

    public static TotalWeightFromStartMessage<TWeight> SendTotalWeight<TWeight>(IActorId senderId, TWeight? totalWeight)
        where TWeight : struct, IComparable<TWeight>, IAdditionOperators<TWeight, TWeight, TWeight>, IAdditiveIdentity<TWeight, TWeight>
        => new(senderId, totalWeight);

    public static StartedWorkMessage WorkStarted(IActorId senderId, Guid taskId)
        => new(senderId, taskId);

    public static FinishedWorkMessage WorkFinished(IActorId senderId, Guid taskId)
        => new(senderId, taskId);

    public static NoNeighboursMessage<TValue> NoNeighbours<TValue>(IActorId senderId, TValue actorNodeValue)
        => new(senderId, actorNodeValue);

    public static RunFinishedMessage RunFinished(IActorId senderId)
        => new(senderId);

    public static RunFinishedImmediatelyMessage<TValue> RunFinishedImmediately<TValue>(IActorId senderId, TValue startValue)
        => new(senderId, startValue);
}