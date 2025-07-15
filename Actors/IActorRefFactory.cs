namespace Actors;

public interface IActorRefFactory<TId, TMessageType, TActorRef>
    where TMessageType : notnull, IMessageType<TId>
    where TId : notnull, IActorId<TId>
    where TActorRef : notnull, IActorRef<TId, TMessageType, TActorRef>
{
    TActorRef Create(Func<TMessageType, ValueTask> send);
}
