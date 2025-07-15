using Actors;

namespace ActorGraphs;

public readonly record struct BreadthFirstSearchRunnerActorRef : IActorRef<BreadthFirstSearchRunnerId, BreadthFirstSearchRunnerMessage, BreadthFirstSearchRunnerActorRef>
{
    private readonly Func<BreadthFirstSearchRunnerMessage, ValueTask> _send;
    public BreadthFirstSearchRunnerId Id { get; }

    [Obsolete("You should not use the default constructor to create an empty breadth-first search runner actor reference.", error: true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public BreadthFirstSearchRunnerActorRef()
    {

    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public BreadthFirstSearchRunnerActorRef(BreadthFirstSearchRunnerId id, Func<BreadthFirstSearchRunnerMessage, ValueTask> send)
    {
        _send = send;
        Id = id;
    }

    public static BreadthFirstSearchRunnerActorRef Create(BreadthFirstSearchRunnerId id, Func<BreadthFirstSearchRunnerMessage, ValueTask> send)
        => new(id, send);

    public ValueTask SendAsync(BreadthFirstSearchRunnerMessage message)
        => _send(message);
}
