namespace Actors;

public interface IActorRef<TId, TMessageType>
    where TMessageType : notnull, IMessageType<TId>
    where TId : notnull, IActorId<TId>
{
    TId Id { get; }
    ValueTask SendAsync(TMessageType message);
}
