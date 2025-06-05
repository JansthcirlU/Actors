using System.Collections.Frozen;
using System.Numerics;
using Actors;
using Graphs;
using Microsoft.Extensions.Logging;

namespace ActorGraphs;

public sealed class BreadthFirstSearchRunner<TNode, TValue, TEdge, TWeight> : ActorBase<BreadthFirstSearchRunnerId, BreadthFirstSearchRunnerMessage>
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TEdge : struct, IDirectedEdge<TNode, TValue, TWeight, TEdge>
    where TValue : struct, IEquatable<TValue>
    where TWeight : struct, IComparable<TWeight>, IAdditionOperators<TWeight, TWeight, TWeight>, IAdditiveIdentity<TWeight, TWeight>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<TValue, TWeight> _breadthFirstSearchDistances;
    private readonly Dictionary<(BreadthFirstSearchActorId, Guid), BreadthFirstSearchRunnerMessage.StartedWorkMessage> _pendingWork;
    private FrozenDictionary<TNode, BreadthFirstSearchActorId>? NodeActorIds { get; set; }
    public FrozenDictionary<BreadthFirstSearchActorId, BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight>>? NodeActors { get; private set; }

    public BreadthFirstSearchRunner(ILoggerFactory loggerFactory)
        : base(BreadthFirstSearchRunnerId.New(), loggerFactory.CreateLogger<BreadthFirstSearchRunner<TNode, TValue, TEdge, TWeight>>())
    {
        _loggerFactory = loggerFactory;
        _pendingWork = [];
        _breadthFirstSearchDistances = [];
    }

    public Task RunBreadthFirstSearchFrom(TNode start)
    {
        throw new NotImplementedException();        
    }

    public void LoadGraph(DirectedGraph<TNode, TValue, TEdge, TWeight> graph)
    {
        // Create actors for each node
        FrozenDictionary<BreadthFirstSearchActorId, BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight>> nodeActors = graph
            .Nodes
            .AsParallel()
            .Select(node =>
            {
                BreadthFirstSearchActorId actorId = BreadthFirstSearchActorId.New();
                ILogger logger = _loggerFactory.CreateLogger<BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight>>();
                BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight> actor = new(actorId, this, node, logger);
                return (actorId, actor);
            })
            .ToFrozenDictionary(
                t => t.actorId,
                t => t.actor);

        // Map actors dictionary to simpler lookup table by id
        FrozenDictionary<TNode, BreadthFirstSearchActorId> nodeActorIds = nodeActors
            .ToFrozenDictionary(
                kvp => kvp.Value.Node,
                kvp => kvp.Value.Id
            );

        // Link node actors to their neighbours
        Parallel.ForEach(
            nodeActorIds,
            kvp =>
            {
                // Get outgoing edges
                graph.TryGetOutgoingEdges(kvp.Key, out IEnumerable<TEdge>? outgoingEdges);

                // Create weights dictionary to each destination
                FrozenDictionary<TNode, TWeight> weights = outgoingEdges!.ToFrozenDictionary(edge => edge.Destination, edge => edge.Weight);

                // Convert weights dictionary to neighbour info dictionary
                FrozenDictionary<BreadthFirstSearchActorId, NeighbourInfo<TNode, TValue, TEdge, TWeight>> neighbours = weights
                    .AsParallel()
                    .Select(weight =>
                    {
                        BreadthFirstSearchActorId neighbourId = nodeActorIds[weight.Key];
                        BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight> neighbour = nodeActors[neighbourId];
                        NeighbourInfo<TNode, TValue, TEdge, TWeight> info = new(neighbour, weight.Value);
                        return (neighbour.Id, info);
                    })
                    .ToFrozenDictionary(
                        t => t.Id,
                        t => t.info
                    );

                // Set neighbours for current actor
                BreadthFirstSearchActorId actorId = nodeActorIds[kvp.Key];
                nodeActors[actorId].InitialiseNeighbours(neighbours);
            });

        // Save list of initialised node actors
        NodeActorIds = nodeActorIds;
        NodeActors = nodeActors;
    }

    protected override Task HandleMessageAsync(BreadthFirstSearchRunnerMessage message)
        => message switch
        {
            BreadthFirstSearchRunnerMessage.TotalWeightFromStartMessage<TWeight> totalWeightFromStartMessage => HandleTotalWeightFromStartMessageAsync(totalWeightFromStartMessage),
            BreadthFirstSearchRunnerMessage.StartedWorkMessage startedWorkMessage => HandleStartedWorkMessageAsync(startedWorkMessage),
            BreadthFirstSearchRunnerMessage.FinishedWorkMessage finishedWorkMessage => HandleFinishedWorkMessageAsync(finishedWorkMessage),
            _ => Task.CompletedTask
        };

    private Task HandleTotalWeightFromStartMessageAsync(BreadthFirstSearchRunnerMessage.TotalWeightFromStartMessage<TWeight> totalWeightFromStartMessage)
    {
        // Check if sender is known node actor
        if (totalWeightFromStartMessage.SenderId is not BreadthFirstSearchActorId actorId) return Task.CompletedTask;
        if (NodeActors?.ContainsKey(actorId) != true) return Task.CompletedTask;


        // Only store associated distance if not null (i.e. node should be reachable from start)
        if (totalWeightFromStartMessage.TotalWeight is TWeight newWeight)
        {
            TValue actorNode = NodeActors[actorId].Node.Value;
            if (!_breadthFirstSearchDistances.TryGetValue(actorNode, out TWeight knownWeight) || newWeight.CompareTo(knownWeight) < 0)
            {
                _breadthFirstSearchDistances[actorNode] = newWeight;
            }
        }

        return Task.CompletedTask;
    }

    private Task HandleStartedWorkMessageAsync(BreadthFirstSearchRunnerMessage.StartedWorkMessage startedWorkMessage)
    {
        // Check if sender is a known node actor
        if (startedWorkMessage.SenderId is not BreadthFirstSearchActorId actorId) return Task.CompletedTask;
        if (NodeActors?.ContainsKey(actorId) != true) return Task.CompletedTask;

        _pendingWork[(actorId, startedWorkMessage.TaskId)] = startedWorkMessage;
        return Task.CompletedTask;
    }

    private async Task HandleFinishedWorkMessageAsync(BreadthFirstSearchRunnerMessage.FinishedWorkMessage finishedWorkMessage)
    {
        // Check if sender is a known node actor
        if (finishedWorkMessage.SenderId is not BreadthFirstSearchActorId actorId) return;
        if (NodeActors?.ContainsKey(actorId) != true) return;

        if (_pendingWork.Remove((actorId, finishedWorkMessage.TaskId)) && _pendingWork.Count == 0)
        {
            Task[] requestTotalWeightTasks = NodeActors
                .Values
                .AsParallel()
                .Select(actor => actor.SendAsync(BreadthFirstSearchMessage.GetTotalWeight(Id)).AsTask())
                .ToArray();
            await Task.WhenAll(requestTotalWeightTasks);
        }
    }

    protected override async ValueTask DisposeActorAsync()
    {
        Task[] disposeActorTasks = NodeActors?
            .Values
            .AsParallel()
            .Select(actor => actor.DisposeAsync().AsTask())
            .ToArray() ?? [];
        await Task.WhenAll(disposeActorTasks);
        NodeActors = null;
    }
}
