using Actors;

namespace ActorGraphs;

public abstract record BreadthFirstSearchMessage(IActorId SenderId) : IMessageType<BreadthFirstSearchActorId>
{
    public sealed record StartBreadthFirstSearchMessage<TValue>(IActorId SenderId, TValue StartValue) : BreadthFirstSearchMessage(SenderId);
    public sealed record UpdateWeightMessage<TValue, TWeight>(IActorId SenderId, TValue StartValue, TWeight TotalWeightFromStart) : BreadthFirstSearchMessage(SenderId);
    public sealed record GetTotalWeightFromStartMessage(IActorId SenderId) : BreadthFirstSearchMessage(SenderId);

    public static StartBreadthFirstSearchMessage<TValue> StartFrom<TValue>(IActorId senderId, TValue startValue)
        => new(senderId, startValue);

    public static UpdateWeightMessage<TValue, TWeight> UpdateTotalWeight<TValue, TWeight>(IActorId senderId, TValue startValue, TWeight totalWeightFromStart)
        => new(senderId, startValue, totalWeightFromStart);

    public static GetTotalWeightFromStartMessage GetTotalWeight(IActorId senderId)
        => new(senderId);
}
