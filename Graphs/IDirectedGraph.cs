namespace Graphs;

public interface IReadonlyDirectedGraph<TNode, TValue, TEdge, TWeight>
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TEdge : struct, IDirectedEdge<TNode, TValue, TWeight, TEdge>
    where TValue : struct, IEquatable<TValue>
    where TWeight : struct, IComparable<TWeight>
{
    public IEnumerable<TNode> Nodes { get; }
    public IEnumerable<TEdge> Edges { get; }

    TNode? FindByValue(TValue @value);
    TEdge? FindEdge(TNode from, TNode to);
    bool TryGetOutgoingEdges(TNode node, out IEnumerable<TEdge>? adjacentEdges);
}

public interface IDirectedGraph<TNode, TValue, TEdge, TWeight> : IReadonlyDirectedGraph<TNode, TValue, TEdge, TWeight>
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TEdge : struct, IDirectedEdge<TNode, TValue, TWeight, TEdge>
    where TValue : struct, IEquatable<TValue>
    where TWeight : struct, IComparable<TWeight>
{
    bool TryAddNode(TValue @value, out TNode? added);
    bool TryRemoveNode(TNode node, out IEnumerable<TEdge>? removedEdges);
    bool TryAddEdge(TNode from, TNode to, TWeight weight, out TEdge? added);
    bool TryRemoveEdge(TEdge edge);
    void Clear();
}
