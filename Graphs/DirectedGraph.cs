namespace Graphs;

public class DirectedGraph<TNode, TValue, TEdge, TWeight> : IDirectedGraph<TNode, TValue, TEdge, TWeight>
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TEdge : struct, IDirectedEdge<TNode, TValue, TWeight, TEdge>
    where TValue : struct, IEquatable<TValue>
    where TWeight : struct, IComparable<TWeight>
{
    private readonly Dictionary<TNode, Dictionary<TNode, TEdge>> _adjacencyList = [];
    private readonly Dictionary<TValue, TNode> _nodesByValue = [];

    public IEnumerable<TNode> Nodes => _adjacencyList.Keys;
    public IEnumerable<TEdge> Edges => _adjacencyList.Values.SelectMany(d => d.Values);

    public void Clear()
    {
        _adjacencyList.Clear();
        _nodesByValue.Clear();
    }

    public TNode? FindByValue(TValue value)
    {
        if (_nodesByValue.TryGetValue(value, out TNode node))
        {
            return node;
        }
        return null;
    }

    public TEdge? FindEdge(TNode from, TNode to)
    {
        if (_adjacencyList.TryGetValue(from, out Dictionary<TNode, TEdge>? destinations) && destinations.TryGetValue(to, out TEdge edge))
        {
            return edge;
        }
        return null;
    }

    public bool TryAddEdge(TNode from, TNode to, TWeight weight, out TEdge? added)
    {
        if (!_adjacencyList.ContainsKey(from) || !_adjacencyList.ContainsKey(to))
        {
            added = null;
            return false;
        }

        if (_adjacencyList[from].ContainsKey(to))
        {
            added = null;
            return false;
        }

        TEdge created = TEdge.Create(from, to, weight);
        _adjacencyList[from][to] = created;

        added = created;
        return true;
    }

    public bool TryAddNode(TValue value, out TNode? added)
    {
        if (_nodesByValue.ContainsKey(value))
        {
            added = null;
            return false;
        }

        TNode created = TNode.Create(value);
        _adjacencyList[created] = [];
        _nodesByValue[value] = created;

        added = created;
        return true;
    }

    public bool TryGetOutgoingEdges(TNode node, out IEnumerable<TEdge>? outgoingEdges)
    {
        if (_adjacencyList.TryGetValue(node, out Dictionary<TNode, TEdge>? destinations))
        {
            outgoingEdges = destinations.Values;
            return true;
        }

        outgoingEdges = null;
        return false;
    }

    public bool TryRemoveEdge(TEdge edge)
    {
        if (_adjacencyList.TryGetValue(edge.Source, out Dictionary<TNode, TEdge>? destinations))
        {
            return destinations.Remove(edge.Destination);
        }
        return false;
    }

    public bool TryRemoveNode(TNode node, out IEnumerable<TEdge>? removedEdges)
    {
        if (!_adjacencyList.TryGetValue(node, out Dictionary<TNode, TEdge>? destinations))
        {
            removedEdges = null;
            return false;
        }

        List<TEdge> allRemovedEdges = destinations.Values.ToList();

        foreach (TNode other in _adjacencyList.Keys.Where(n => !n.Equals(node)))
        {
            if (_adjacencyList[other].TryGetValue(node, out TEdge incoming))
            {
                _adjacencyList[other].Remove(node);
                allRemovedEdges.Add(incoming);
            }
        }

        _adjacencyList.Remove(node);
        _nodesByValue.Remove(node.Value);

        removedEdges = allRemovedEdges;
        return true;
    }
}