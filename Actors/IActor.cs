namespace Actors;

public interface IActor<TId, TMessageType, TActorRef> : IAsyncDisposable
    where TMessageType : notnull, IMessageType<TId>
    where TId : notnull, IActorId<TId>
    where TActorRef : IActorRef<TId, TMessageType, TActorRef>
{
    TActorRef Reference { get; }
}
