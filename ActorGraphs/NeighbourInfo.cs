using System.Numerics;
using Graphs;

namespace ActorGraphs;

public readonly record struct NeighbourInfo<TNode, TValue, TEdge, TWeight>(BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight> Neighbour, TWeight DistanceFromNeighbour)
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TEdge : struct, IDirectedEdge<TNode, TValue, TWeight, TEdge>
    where TValue : struct, IEquatable<TValue>
    where TWeight : struct, IComparable<TWeight>, IAdditionOperators<TWeight, TWeight, TWeight>, IAdditiveIdentity<TWeight, TWeight>;