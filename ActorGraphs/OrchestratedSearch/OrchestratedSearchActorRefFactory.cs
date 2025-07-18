using Actors;
using Graphs;

namespace ActorGraphs.OrchestratedSearch;

public class OrchestratedSearchActorRefFactory<TNode, TValue> : ActorRefFactory<OrchestratedSearchActorId, OrchestratedSearchMessage, OrchestratedSearchActorRef<TNode, TValue>>
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TValue : struct, IEquatable<TValue>
{
    private readonly TNode _node;

    public OrchestratedSearchActorRefFactory(OrchestratedSearchActorId id, TNode node) : base(id)
    {
        _node = node;
    }

    public override OrchestratedSearchActorRef<TNode, TValue> Create(Func<OrchestratedSearchMessage, ValueTask> send)
        => new(Id, _node, send);
}