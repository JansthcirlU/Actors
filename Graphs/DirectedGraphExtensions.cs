using System.Numerics;

namespace Graphs;

public static class DirectedGraphExtensions
{
    public static Dictionary<TNode, TWeight?> BreadthFirstSearch<TNode, TValue, TEdge, TWeight>(this IDirectedGraph<TNode, TValue, TEdge, TWeight> graph, TNode start)
        where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
        where TEdge : struct, IDirectedEdge<TNode, TValue, TWeight, TEdge>, IEquatable<TEdge>
        where TValue : struct, IEquatable<TValue>
        where TWeight : struct, IComparable<TWeight>, IAdditionOperators<TWeight, TWeight, TWeight>, IAdditiveIdentity<TWeight, TWeight>
    {
        Dictionary<TNode, TWeight?> distances = [];
        foreach (TNode node in graph.Nodes)
        {
            distances[node] = null;
        }
        distances[start] = TWeight.AdditiveIdentity;
        throw new NotImplementedException("BFS is not yet implemented.");
    }
}