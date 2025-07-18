using Actors;
using Graphs;

namespace ActorGraphs.OrchestratedSearch;

public readonly record struct OrchestratedSearchActorRef<TNode, TValue> : IActorRef<OrchestratedSearchActorId, OrchestratedSearchMessage, OrchestratedSearchActorRef<TNode, TValue>>
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TValue : struct, IEquatable<TValue>
{
    private readonly Func<OrchestratedSearchMessage, ValueTask> _send;
    public OrchestratedSearchActorId Id { get; }
    public TNode Node { get; }

    [Obsolete(error: true, message: "You should not use the default constructor to create an empty orchestrated search actor reference.")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public OrchestratedSearchActorRef()
    {

    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public OrchestratedSearchActorRef(OrchestratedSearchActorId id, TNode node, Func<OrchestratedSearchMessage, ValueTask> send)
    {
        _send = send;
        Id = id;
        Node = node;
    }

    public ValueTask SendAsync(OrchestratedSearchMessage message)
        => _send(message);
}
