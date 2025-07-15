using Actors;
using Graphs;

namespace ActorGraphs;

public readonly record struct BreadthFirstSearchActorRef<TNode, TValue> : IActorRef<BreadthFirstSearchActorId, BreadthFirstSearchMessage, BreadthFirstSearchActorRef<TNode, TValue>>
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TValue : struct, IEquatable<TValue>
{
    private readonly Func<BreadthFirstSearchMessage, ValueTask> _send;
    public BreadthFirstSearchActorId Id { get; }
    public TNode Node { get; }

    [Obsolete(error: true, message: "You should not use the default constructor to create an empty breadth-first search actor reference.")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public BreadthFirstSearchActorRef()
    {

    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public BreadthFirstSearchActorRef(BreadthFirstSearchActorId id, TNode node, Func<BreadthFirstSearchMessage, ValueTask> send)
    {
        _send = send;
        Id = id;
        Node = node;
    }

    public ValueTask SendAsync(BreadthFirstSearchMessage message)
        => _send(message);
}
