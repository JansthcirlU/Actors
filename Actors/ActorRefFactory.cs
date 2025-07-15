namespace Actors;

public abstract class ActorRefFactory<TId, TMessageType, TActorRef> : IActorRefFactory<TId, TMessageType, TActorRef>
    where TMessageType : notnull, IMessageType<TId>
    where TId : notnull, IActorId<TId>
    where TActorRef : notnull, IActorRef<TId, TMessageType, TActorRef>
{
    public TId Id { get; }

    public ActorRefFactory(TId id)
    {
        Id = id;
    }

    public abstract TActorRef Create(Func<TMessageType, ValueTask> send);
}