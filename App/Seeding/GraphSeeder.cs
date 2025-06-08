using Graphs;

namespace App.Seeding;

public static class GraphSeeder
{
    public readonly record struct IntegerNode(int Value) : INode<int, IntegerNode>
    {
        public static IntegerNode Create(int value)
            => new(value);
    }

    public readonly record struct IntegerEdge(IntegerNode Source, IntegerNode Destination, int Weight) : IDirectedEdge<IntegerNode, int, int, IntegerEdge>
    {
        public static IntegerEdge Create(IntegerNode source, IntegerNode destination, int weight)
            => new(source, destination, weight);
    }

    public static DirectedGraph<IntegerNode, int, IntegerEdge, int> CreateFullyConnectedGraph(int nodes)
    {
        if (nodes < 0) throw new InvalidOperationException("Number of nodes must not be less than zero.");

        DirectedGraph<IntegerNode, int, IntegerEdge, int> graph = new();

        int[] numbers = GetNumbers(nodes).ToArray();

        foreach (int number in numbers)
        {
            graph.TryAddNode(number, out _);
        }

        foreach (int sourceValue in numbers)
        {
            IntegerNode? source = graph.FindByValue(sourceValue);
            if (source is null) continue;

            foreach (int destinationValue in numbers)
            {
                if (sourceValue == destinationValue) continue;

                IntegerNode? destination = graph.FindByValue(destinationValue);
                if (destination is null) continue;

                graph.TryAddEdge(source.Value, destination.Value, Random.Shared.Next(1, 100), out IntegerEdge? _);
            }
        }

        return graph;
    }

    private static IEnumerable<int> GetNumbers(int nodes)
        => Enumerable.Range(1, nodes);
}