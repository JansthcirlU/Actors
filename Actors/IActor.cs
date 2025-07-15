namespace Actors;

public interface IActor<TId, TMessageType> : IAsyncDisposable
    where TMessageType : notnull, IMessageType<TId>
    where TId : notnull, IActorId<TId>
{
    IActorRef<TId, TMessageType> Reference { get; }
}
