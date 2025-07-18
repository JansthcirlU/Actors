using System.Numerics;
using Actors;

namespace ActorGraphs.OrchestratedSearch;

public abstract record OrchestratedSearchRunnerMessage(IActorRef SenderRef) : IMessageType<OrchestratedSearchRunnerId>
{
    public sealed record TotalWeightFromStartMessage<TWeight>(IActorRef SenderRef, TWeight? TotalWeight) : OrchestratedSearchRunnerMessage(SenderRef)
        where TWeight : struct, IComparable<TWeight>, IAdditionOperators<TWeight, TWeight, TWeight>, IAdditiveIdentity<TWeight, TWeight>;
    public sealed record StartedWorkMessage(IActorRef SenderRef, Guid TaskId) : OrchestratedSearchRunnerMessage(SenderRef);
    public sealed record FinishedWorkMessage(IActorRef SenderRef, Guid TaskId) : OrchestratedSearchRunnerMessage(SenderRef);
    public sealed record NoNeighboursMessage<TValue>(IActorRef SenderRef, TValue ActorNodeValue) : OrchestratedSearchRunnerMessage(SenderRef);
    public sealed record RunFinishedMessage(IActorRef SenderRef) : OrchestratedSearchRunnerMessage(SenderRef);
    public sealed record RunFinishedImmediatelyMessage<TValue>(IActorRef SenderRef, TValue StartValue) : OrchestratedSearchRunnerMessage(SenderRef);

    public static TotalWeightFromStartMessage<TWeight> SendTotalWeight<TWeight>(IActorRef senderRef, TWeight? totalWeight)
        where TWeight : struct, IComparable<TWeight>, IAdditionOperators<TWeight, TWeight, TWeight>, IAdditiveIdentity<TWeight, TWeight>
        => new(senderRef, totalWeight);

    public static StartedWorkMessage WorkStarted(IActorRef senderRef, Guid taskId)
        => new(senderRef, taskId);

    public static FinishedWorkMessage WorkFinished(IActorRef senderRef, Guid taskId)
        => new(senderRef, taskId);

    public static NoNeighboursMessage<TValue> NoNeighbours<TValue>(IActorRef senderRef, TValue actorNodeValue)
        => new(senderRef, actorNodeValue);

    public static RunFinishedMessage RunFinished(IActorRef senderRef)
        => new(senderRef);

    public static RunFinishedImmediatelyMessage<TValue> RunFinishedImmediately<TValue>(IActorRef senderRef, TValue startValue)
        => new(senderRef, startValue);
}