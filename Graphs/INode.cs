namespace Graphs;

public interface INode<TValue, TSelf>
    where TValue : struct, IEquatable<TValue>
    where TSelf : INode<TValue, TSelf>, IEquatable<TSelf>
{
    TValue Value { get; }

    static abstract TSelf Create(TValue value);
}
