using Actors;

namespace ActorGraphs;

public abstract record BreadthFirstSearchMessage(IActorRef SenderRef) : IMessageType<BreadthFirstSearchActorId>
{
    public sealed record StartBreadthFirstSearchMessage<TValue>(IActorRef SenderRef, TValue StartValue, Guid KickOffId) : BreadthFirstSearchMessage(SenderRef);
    public sealed record UpdateWeightMessage<TValue, TWeight>(IActorRef SenderRef, TValue StartValue, TWeight TotalWeightFromStart) : BreadthFirstSearchMessage(SenderRef);
    public sealed record GetTotalWeightFromStartMessage(IActorRef SenderRef) : BreadthFirstSearchMessage(SenderRef);
    public sealed record SearchKickedOffMessage(IActorRef SenderRef, Guid KickOffId) : BreadthFirstSearchMessage(SenderRef);

    public static StartBreadthFirstSearchMessage<TValue> StartFrom<TValue>(IActorRef senderRef, TValue startValue, Guid kickOffId)
        => new(senderRef, startValue, kickOffId);

    public static UpdateWeightMessage<TValue, TWeight> UpdateTotalWeight<TValue, TWeight>(IActorRef senderRef, TValue startValue, TWeight totalWeightFromStart)
        => new(senderRef, startValue, totalWeightFromStart);

    public static GetTotalWeightFromStartMessage GetTotalWeightFromStart(IActorRef senderRef)
        => new(senderRef);

    public static SearchKickedOffMessage SearchKickedOff(IActorRef senderRef, Guid kickOffId)
        => new(senderRef, kickOffId);
}
