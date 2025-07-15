// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using ActorGraphs;
using App.Seeding;
using Graphs;
using Microsoft.Extensions.Logging;

Console.WriteLine($"Benchmark started at {DateTime.Now}.");
Console.WriteLine();

Console.WriteLine("Warming up the hot paths with a small graph...");
Console.WriteLine();

using LoggerFactory loggerFactory = new();
Stopwatch sw = new();

// Warmup with small graph
DirectedGraph<GraphSeeder.IntegerNode, int, GraphSeeder.IntegerEdge, int> warmupGraph = GraphSeeder.CreateFullyConnectedGraph(100);
GraphSeeder.IntegerNode? warmupOne = warmupGraph.FindByValue(1);
BreadthFirstSearchRunner<GraphSeeder.IntegerNode, int, GraphSeeder.IntegerEdge, int> warmupSearchRunner = new(loggerFactory);
warmupSearchRunner.LoadGraph(warmupGraph);
await warmupSearchRunner.RunBreadthFirstSearchFrom(warmupOne!.Value, CancellationToken.None);
await warmupSearchRunner.DisposeAsync();
warmupGraph.RunDijkstra(warmupOne.Value);

Console.WriteLine("Starting actual runs...");
Console.WriteLine();

// Benchmark greater node counts
int[] nodeCounts = [100, 200, 400, 800, 1_600, 3_200, 6_400];

foreach (int nodeCount in nodeCounts)
{
    DirectedGraph<GraphSeeder.IntegerNode, int, GraphSeeder.IntegerEdge, int>? fullyConnectedGraph = GraphSeeder.CreateFullyConnectedGraph(nodeCount);
    GraphSeeder.IntegerNode? one = fullyConnectedGraph.FindByValue(1);

    if (one is null) return;

    IReadOnlyDictionary<int, int> distancesConcurrent;
    await using (BreadthFirstSearchRunner<GraphSeeder.IntegerNode, int, GraphSeeder.IntegerEdge, int> searchRunner = new(loggerFactory))
    {
        // Load graph and run search
        sw.Restart();
        searchRunner.LoadGraph(fullyConnectedGraph);
        sw.Stop();

        TimeSpan timeToLoad = sw.Elapsed;
        Console.WriteLine($"Time to load a fully connected graph with {nodeCount} nodes: {timeToLoad}");

        sw.Restart();
        distancesConcurrent = await searchRunner.RunBreadthFirstSearchFrom(one.Value, CancellationToken.None);
        sw.Stop();

        TimeSpan timeToSearchConcurrently = sw.Elapsed;
        Console.WriteLine($"Time to run concurrent search: {timeToSearchConcurrently}");
    }

    // Search graph synchronously
    sw.Restart();
    IReadOnlyDictionary<int, int> distancesSynchronous = fullyConnectedGraph.RunDijkstra(one.Value);
    sw.Stop();

    TimeSpan timeToSearchSynchronously = sw.Elapsed;
    Console.WriteLine($"Time to run synchronous search: {timeToSearchSynchronously}");
    Console.WriteLine();

    Console.WriteLine("Checking distance dictionary equality...");

    // Check if both distance dictionaries contain the same information
    if (distancesConcurrent.Count == distancesSynchronous.Count &&
        distancesConcurrent.All(kvp => distancesSynchronous.TryGetValue(kvp.Key, out int v) && v == kvp.Value))
    {
        Console.WriteLine("Invalid results, stopping benchmark...");
        Console.WriteLine();
        break;
    }

    Console.WriteLine("Both techniques yielded the same distances!");
    Console.WriteLine();

    fullyConnectedGraph = null;
}

Console.WriteLine($"Benchmark finished at {DateTime.Now}.");