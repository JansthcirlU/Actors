using Actors;

namespace ActorGraphs.OrchestratedSearch;

public abstract record OrchestratedSearchMessage(IActorRef SenderRef) : IMessageType<OrchestratedSearchActorId>
{
    public sealed record StartOrchestratedSearchMessage<TValue>(IActorRef SenderRef, TValue StartValue, Guid KickOffId) : OrchestratedSearchMessage(SenderRef);
    public sealed record UpdateWeightMessage<TValue, TWeight>(IActorRef SenderRef, TValue StartValue, TWeight TotalWeightFromStart) : OrchestratedSearchMessage(SenderRef);
    public sealed record GetTotalWeightFromStartMessage(IActorRef SenderRef) : OrchestratedSearchMessage(SenderRef);
    public sealed record SearchKickedOffMessage(IActorRef SenderRef, Guid KickOffId) : OrchestratedSearchMessage(SenderRef);

    public static StartOrchestratedSearchMessage<TValue> StartFrom<TValue>(IActorRef senderRef, TValue startValue, Guid kickOffId)
        => new(senderRef, startValue, kickOffId);

    public static UpdateWeightMessage<TValue, TWeight> UpdateTotalWeight<TValue, TWeight>(IActorRef senderRef, TValue startValue, TWeight totalWeightFromStart)
        => new(senderRef, startValue, totalWeightFromStart);

    public static GetTotalWeightFromStartMessage GetTotalWeightFromStart(IActorRef senderRef)
        => new(senderRef);

    public static SearchKickedOffMessage SearchKickedOff(IActorRef senderRef, Guid kickOffId)
        => new(senderRef, kickOffId);
}
