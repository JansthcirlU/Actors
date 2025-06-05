namespace Actors;

public interface IActor<TId, TMessageType> : IAsyncDisposable
    where TMessageType : notnull, IMessageType<TId>
    where TId : notnull, IActorId<TId>
{
    TId Id { get; }

    /// <summary>Sends a message to this actor with "best effort" delivery.</summary>
    ValueTask SendAsync(TMessageType message);
}
