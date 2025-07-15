using Actors;

namespace ActorGraphs;

public abstract record BreadthFirstSearchMessage(IActorRef SenderRef) : IMessageType<BreadthFirstSearchActorId>
{
    public sealed record StartBreadthFirstSearchMessage<TValue>(IActorRef SenderRef, TValue StartValue) : BreadthFirstSearchMessage(SenderRef);
    public sealed record UpdateWeightMessage<TValue, TWeight>(IActorRef SenderRef, TValue StartValue, TWeight TotalWeightFromStart) : BreadthFirstSearchMessage(SenderRef);
    public sealed record GetTotalWeightFromStartMessage(IActorRef SenderRef) : BreadthFirstSearchMessage(SenderRef);

    public static StartBreadthFirstSearchMessage<TValue> StartFrom<TValue>(IActorRef senderRef, TValue startValue)
        => new(senderRef, startValue);

    public static UpdateWeightMessage<TValue, TWeight> UpdateTotalWeight<TValue, TWeight>(IActorRef senderRef, TValue startValue, TWeight totalWeightFromStart)
        => new(senderRef, startValue, totalWeightFromStart);

    public static GetTotalWeightFromStartMessage GetTotalWeightFromStart(IActorRef senderRef)
        => new(senderRef);
}
