using System.Numerics;

namespace Graphs;

public static class DijkstraSearchRunner
{
    public static IReadOnlyDictionary<TValue, TWeight> RunDijkstra<TNode, TValue, TEdge, TWeight>(this IDirectedGraph<TNode, TValue, TEdge, TWeight> graph, TNode start)
        where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
        where TEdge : struct, IDirectedEdge<TNode, TValue, TWeight, TEdge>, IEquatable<TEdge>
        where TValue : struct, IEquatable<TValue>
        where TWeight : struct, IComparable<TWeight>, IAdditionOperators<TWeight, TWeight, TWeight>, IAdditiveIdentity<TWeight, TWeight>
    {
        // Dictionary to store discovered destinations
        Dictionary<TValue, TWeight> distances = new()
        {
            [start.Value] = TWeight.AdditiveIdentity
        };

        // Enqueue and dequeue by minimum next distance
        PriorityQueue<TNode, TWeight> priorityQueue = new();
        priorityQueue.Enqueue(start, TWeight.AdditiveIdentity);

        while (priorityQueue.TryDequeue(out TNode nextNode, out TWeight weightToNextNode))
        {
            // Skip out of sync next node candidate
            if (!weightToNextNode.Equals(distances[nextNode.Value])) continue;

            // Get next edges
            if (!graph.TryGetOutgoingEdges(nextNode, out IEnumerable<TEdge>? outgoing)) continue;

            foreach (var e in outgoing!)
            {
                // Set current distance to known reachable destination
                var destination = e.Destination;
                var candidateTotalWeight = weightToNextNode + e.Weight;

                if (!distances.TryGetValue(destination.Value, out TWeight currentTotalWeight) || candidateTotalWeight.CompareTo(currentTotalWeight) < 0)
                {
                    distances[destination.Value] = candidateTotalWeight;
                    priorityQueue.Enqueue(destination, candidateTotalWeight);
                }
            }
        }

        return distances;
    }
}