using Actors;
using Graphs;

namespace ActorGraphs;

public class BreadthFirstSearchActorRefFactory<TNode, TValue> : ActorRefFactory<BreadthFirstSearchActorId, BreadthFirstSearchMessage, BreadthFirstSearchActorRef<TNode, TValue>>
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TValue : struct, IEquatable<TValue>
{
    private readonly TNode _node;

    public BreadthFirstSearchActorRefFactory(BreadthFirstSearchActorId id, TNode node) : base(id)
    {
        _node = node;
    }

    public override BreadthFirstSearchActorRef<TNode, TValue> Create(Func<BreadthFirstSearchMessage, ValueTask> send)
        => new(Id, _node, send);
}