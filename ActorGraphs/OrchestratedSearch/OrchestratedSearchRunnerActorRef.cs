using Actors;

namespace ActorGraphs.OrchestratedSearch;

public readonly record struct OrchestratedSearchRunnerActorRef : IActorRef<OrchestratedSearchRunnerId, OrchestratedSearchRunnerMessage, OrchestratedSearchRunnerActorRef>
{
    private readonly Func<OrchestratedSearchRunnerMessage, ValueTask> _send;
    public OrchestratedSearchRunnerId Id { get; }

    [Obsolete("You should not use the default constructor to create an empty orchestrated search runner actor reference.", error: true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public OrchestratedSearchRunnerActorRef()
    {

    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public OrchestratedSearchRunnerActorRef(OrchestratedSearchRunnerId id, Func<OrchestratedSearchRunnerMessage, ValueTask> send)
    {
        _send = send;
        Id = id;
    }

    public static OrchestratedSearchRunnerActorRef Create(OrchestratedSearchRunnerId id, Func<OrchestratedSearchRunnerMessage, ValueTask> send)
        => new(id, send);

    public ValueTask SendAsync(OrchestratedSearchRunnerMessage message)
        => _send(message);
}
