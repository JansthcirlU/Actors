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
    private bool _workInitiated;
    private TaskCompletionSource<bool>? _runCompletionSource;
    private FrozenDictionary<TNode, BreadthFirstSearchActorId>? NodeActorIds { get; set; }
    public FrozenDictionary<BreadthFirstSearchActorId, BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight>>? NodeActors { get; private set; }

    public BreadthFirstSearchRunner(ILoggerFactory loggerFactory)
        : base(BreadthFirstSearchRunnerId.New(), loggerFactory.CreateLogger<BreadthFirstSearchRunner<TNode, TValue, TEdge, TWeight>>())
    {
        _loggerFactory = loggerFactory;
        _pendingWork = [];
        _breadthFirstSearchDistances = [];
    }

    public async Task<IReadOnlyDictionary<TValue, TWeight>?> RunBreadthFirstSearchFrom(TNode start, CancellationToken cancellationToken)
    {
        // Create task completion source
        _runCompletionSource = new();
        using CancellationTokenRegistration registration = cancellationToken.Register(() => _runCompletionSource.TrySetCanceled());

        // Find starting node actor
        if (NodeActorIds?.TryGetValue(start, out BreadthFirstSearchActorId startNodeActorId) != true) return null;
        if (NodeActors?.TryGetValue(startNodeActorId, out BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight>? startNodeActor) != true) return null;

        // Start node actor was found, kick off run
        _workInitiated = true;
        BreadthFirstSearchMessage.StartBreadthFirstSearchMessage<TValue> startSearchMessage = BreadthFirstSearchMessage.StartFrom(Id, start.Value);
        await startNodeActor!.SendAsync(startSearchMessage);

        // Wait till task completion source finishes
        await _runCompletionSource.Task;

        // Return distances
        return _breadthFirstSearchDistances.AsReadOnly();
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
            BreadthFirstSearchRunnerMessage.NoNeighboursMessage<TValue> noNeighboursMessage => HandleNoNeighboursMessageAsync(noNeighboursMessage),
            BreadthFirstSearchRunnerMessage.RunFinishedMessage runFinishedMessage => HandleRunFinishedMessageAsync(runFinishedMessage),
            BreadthFirstSearchRunnerMessage.RunFinishedImmediatelyMessage<TValue> runFinishedImmediatelyMessage => HandleRunFinishedImmediatelyMessageAsync(runFinishedImmediatelyMessage),
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

        // When there's no more pending work, prepare to finish the run
        if (_pendingWork.Remove((actorId, finishedWorkMessage.TaskId)) && _pendingWork.Count == 0 && _workInitiated)
        {
            // Ask each node actor to send back its shortest path weight from the start node
            foreach (BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight> actor in NodeActors.Values)
            {
                _ = actor.SendAsync(BreadthFirstSearchMessage.GetTotalWeightFromStart(Id));
            }

            // After all actors have sent their weight, signal self to finish run
                BreadthFirstSearchRunnerMessage.RunFinishedMessage runFinishedMessage =
                BreadthFirstSearchRunnerMessage.RunFinished(Id);
            await SendAsync(runFinishedMessage);
        }
    }

    private async Task HandleNoNeighboursMessageAsync(BreadthFirstSearchRunnerMessage.NoNeighboursMessage<TValue> noNeighboursMessage)
    {
        // Check if sender is a known node actor
        if (noNeighboursMessage.SenderId is not BreadthFirstSearchActorId actorId) return;
        if (NodeActors?.TryGetValue(actorId, out BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight>? actor) != true) return;

        if (_workInitiated)
        {
            BreadthFirstSearchRunnerMessage.RunFinishedImmediatelyMessage<TValue> runFinishedImmediatelyMessage =
                BreadthFirstSearchRunnerMessage.RunFinishedImmediately(Id, noNeighboursMessage.ActorNodeValue);
            await SendAsync(runFinishedImmediatelyMessage);
        }
    }

    private Task HandleRunFinishedMessageAsync(BreadthFirstSearchRunnerMessage.RunFinishedMessage runFinishedMessage)
    {
        // Check if message comes from self
        if (runFinishedMessage.SenderId is not BreadthFirstSearchRunnerId selfId) return Task.CompletedTask;
        if (!selfId.Equals(Id)) return Task.CompletedTask;

        // Internally signal run completion
        _runCompletionSource?.TrySetResult(true);
        return Task.CompletedTask;
    }

    private Task HandleRunFinishedImmediatelyMessageAsync(BreadthFirstSearchRunnerMessage.RunFinishedImmediatelyMessage<TValue> runFinishedImmediatelyMessage)
    {
        // Check if message comes from self
        if (runFinishedImmediatelyMessage.SenderId is not BreadthFirstSearchRunnerId selfId) return Task.CompletedTask;
        if (!selfId.Equals(Id)) return Task.CompletedTask;

        // Update shortest-path distances
        _breadthFirstSearchDistances.TryAdd(runFinishedImmediatelyMessage.StartValue, TWeight.AdditiveIdentity);

        // Internally signal run completion
        _runCompletionSource?.TrySetResult(true);
        return Task.CompletedTask;
    }

    protected override async ValueTask DisposeActorAsync()
    {
        Task[] disposeActorTasks = NodeActors?
            .Values
            .Select(actor => actor.DisposeAsync().AsTask())
            .ToArray() ?? [];
        await Task.WhenAll(disposeActorTasks);

        _runCompletionSource = null;
        NodeActorIds = null;
        NodeActors = null;
    }
}
