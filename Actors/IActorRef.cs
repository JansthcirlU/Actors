namespace Actors;

public interface IActorRef
{
    IActorId Id { get; }
}
public interface IActorRef<TId, TMessageType, TSelf> : IActorRef
    where TMessageType : notnull, IMessageType<TId>
    where TId : notnull, IActorId<TId>
    where TSelf : IActorRef<TId, TMessageType, TSelf>
{
    new TId Id { get; }
    IActorId IActorRef.Id => Id;
    ValueTask SendAsync(TMessageType message);
}
