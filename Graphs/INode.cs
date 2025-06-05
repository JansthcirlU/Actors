namespace Graphs;

public interface INode<TValue, TNode>
    where TValue : struct, IEquatable<TValue>
    where TNode : INode<TValue, TNode>, IEquatable<TNode>
{
    TValue Value { get; }

    static abstract TNode Create(TValue value);
}
