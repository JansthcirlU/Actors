namespace Actors;

internal readonly record struct ActorRef<TId, TMessageType> : IActorRef<TId, TMessageType>
    where TMessageType : notnull, IMessageType<TId>
    where TId : notnull, IActorId<TId>
{
    private readonly Func<TMessageType, ValueTask> _send;
    public TId Id { get; }

    [Obsolete("You should not use the default constructor to create an empty actor reference.", error: true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ActorRef()
    {

    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ActorRef(TId id, Func<TMessageType, ValueTask> send)
    {
        Id = id;
        _send = send;
    }


    public ValueTask SendAsync(TMessageType message)
        => _send(message);
}