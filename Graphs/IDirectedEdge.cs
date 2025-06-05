namespace Graphs;

public interface IDirectedEdge<TNode, TValue, TWeight, TSelf>
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TValue : struct, IEquatable<TValue>
    where TWeight : struct, IComparable<TWeight>
    where TSelf : struct, IDirectedEdge<TNode, TValue, TWeight, TSelf>
{
    TNode Source { get; }
    TNode Destination { get; }
    TWeight Weight { get; }

    static abstract TSelf Create(TNode source, TNode destination, TWeight weight);
}
