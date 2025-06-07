// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using ActorGraphs;
using App.Seeding;
using Graphs;
using Microsoft.Extensions.Logging;

Console.WriteLine("Hello, World!");

LoggerFactory loggerFactory = new();
DirectedGraph<GraphSeeder.IntegerNode, int, GraphSeeder.IntegerEdge, int> fullyConnectedGraph = GraphSeeder.CreateFullyConnectedGraph(20);
GraphSeeder.IntegerNode? one = fullyConnectedGraph.FindByValue(1);
BreadthFirstSearchRunner<GraphSeeder.IntegerNode, int, GraphSeeder.IntegerEdge, int> searchRunner = new(loggerFactory);

if (one is null) return;

// Load graph and run search
searchRunner.LoadGraph(fullyConnectedGraph);
IReadOnlyDictionary<int, int>? distances = await searchRunner.RunBreadthFirstSearchFrom(one.Value, CancellationToken.None);

Debugger.Break();