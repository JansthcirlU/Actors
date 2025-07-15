namespace Actors;

public interface IActorRef<TId, TMessageType, TSelf>
    where TMessageType : notnull, IMessageType<TId>
    where TId : notnull, IActorId<TId>
    where TSelf : IActorRef<TId, TMessageType, TSelf>
{
    abstract static TSelf Create(TId id, Func<TMessageType, ValueTask> send);
    TId Id { get; }
    ValueTask SendAsync(TMessageType message);
}
