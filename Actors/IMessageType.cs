namespace Actors;

public interface IMessageType<TReceiverId>
    where TReceiverId : notnull, IActorId<TReceiverId>
{
    IActorRef SenderRef { get; }
}
