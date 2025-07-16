using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Numerics;
using Actors;
using Graphs;
using Microsoft.Extensions.Logging;

namespace ActorGraphs;

public sealed class BreadthFirstSearchRunner<TNode, TValue, TEdge, TWeight> : ActorBase<BreadthFirstSearchRunnerId, BreadthFirstSearchRunnerMessage, BreadthFirstSearchRunnerActorRef>
    where TNode : struct, INode<TValue, TNode>, IEquatable<TNode>
    where TEdge : struct, IDirectedEdge<TNode, TValue, TWeight, TEdge>
    where TValue : struct, IEquatable<TValue>
    where TWeight : struct, IComparable<TWeight>, IAdditionOperators<TWeight, TWeight, TWeight>, IAdditiveIdentity<TWeight, TWeight>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<TValue, TWeight> _breadthFirstSearchDistances;
    private int _pendingWeightResponses;
    private readonly ConcurrentDictionary<(BreadthFirstSearchActorRef<TNode, TValue>, Guid), BreadthFirstSearchRunnerMessage.StartedWorkMessage> _pendingWork;
    private bool _workInitiated;
    private bool _searchKickOffConfirmed;
    private Guid? _kickOffId;
    private TaskCompletionSource<bool>? _runCompletionSource;
    private FrozenSet<BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight>>? _nodeActors;
    private FrozenSet<BreadthFirstSearchActorRef<TNode, TValue>>? _nodeActorReferences;
    private BreadthFirstSearchActorRef<TNode, TValue>? _startRef;

    public BreadthFirstSearchRunner(ILoggerFactory loggerFactory)
        : base(new BreadthFirstSearchRunnerActorRefFactory(BreadthFirstSearchRunnerId.New()), loggerFactory.CreateLogger<BreadthFirstSearchRunner<TNode, TValue, TEdge, TWeight>>())
    {
        _loggerFactory = loggerFactory;
        _pendingWork = [];
        _breadthFirstSearchDistances = [];
    }

    public async Task<IReadOnlyDictionary<TValue, TWeight>> RunBreadthFirstSearchFrom(TNode start, CancellationToken cancellationToken)
    {
        // Create task completion source
        _runCompletionSource = new();
        using CancellationTokenRegistration registration = cancellationToken.Register(() => _runCompletionSource.TrySetCanceled());

        // Find starting node actor
        if (_nodeActorReferences?.SingleOrDefault(actorRef => actorRef.Node.Equals(start)) is not BreadthFirstSearchActorRef<TNode, TValue> startRef) return FrozenDictionary<TValue, TWeight>.Empty;
        _startRef = startRef;

        // Start node actor was found, kick off run
        _workInitiated = true;
        _searchKickOffConfirmed = false;
        _kickOffId = Guid.NewGuid();
        BreadthFirstSearchMessage.StartBreadthFirstSearchMessage<TValue> startSearchMessage = BreadthFirstSearchMessage.StartFrom(Reference, start.Value, kickOffId: _kickOffId.Value);
        await _startRef.Value.SendAsync(startSearchMessage);

        // Wait till task completion source finishes
        await _runCompletionSource.Task;

        // Return distances
        return _breadthFirstSearchDistances.ToFrozenDictionary();
    }

    public void LoadGraph(DirectedGraph<TNode, TValue, TEdge, TWeight> graph)
    {
        // Initialize actors and references
        _nodeActors = graph
            .Nodes
            .AsParallel()
            .Select(n =>
            {
                BreadthFirstSearchActorId actorId = BreadthFirstSearchActorId.New();
                ILogger logger = _loggerFactory.CreateLogger<BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight>>();
                BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight> actor = new(actorId, Reference, n, logger);
                return actor;
            }).ToFrozenSet();
        _nodeActorReferences = _nodeActors
            .Select(a => a.Reference)
            .ToFrozenSet();

        // Create actor reference dictionary
        FrozenDictionary<TNode, BreadthFirstSearchActor<TNode, TValue, TEdge, TWeight>> nodeActorRefs = _nodeActors
            .ToFrozenDictionary(
                actor => actor.Node,
                actor => actor
            );

        // Link actors to neighbor actors
        Parallel.ForEach(
            nodeActorRefs,
            kvp =>
            {
                graph.TryGetOutgoingEdges(kvp.Key, out IEnumerable<TEdge>? outgoingEdges);
                FrozenDictionary<BreadthFirstSearchActorRef<TNode, TValue>, TWeight> neighbourDistances = outgoingEdges?
                    .ToFrozenDictionary(
                        edge => nodeActorRefs[edge.Destination].Reference,
                        edge => edge.Weight
                    ) ?? FrozenDictionary<BreadthFirstSearchActorRef<TNode, TValue>, TWeight>.Empty;
                kvp.Value.InitialiseNeighbours(neighbourDistances);
            });
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

    private async Task HandleTotalWeightFromStartMessageAsync(BreadthFirstSearchRunnerMessage.TotalWeightFromStartMessage<TWeight> totalWeightFromStartMessage)
    {
        // Check if sender is known node actor
        if (totalWeightFromStartMessage.SenderRef is not BreadthFirstSearchActorRef<TNode, TValue> senderRef || _nodeActorReferences?.Contains(senderRef) != true) return;


        // Only store associated distance if not null (i.e. node should be reachable from start)
        if (totalWeightFromStartMessage.TotalWeight is TWeight newWeight)
        {
            TValue actorNode = senderRef.Node.Value;
            if (!_breadthFirstSearchDistances.TryGetValue(actorNode, out TWeight knownWeight) || newWeight.CompareTo(knownWeight) < 0)
            {
                _breadthFirstSearchDistances[actorNode] = newWeight;
            }
        }

        if (--_pendingWeightResponses == 0) await Reference.SendAsync(BreadthFirstSearchRunnerMessage.RunFinished(Reference));
    }

    private async Task HandleStartedWorkMessageAsync(BreadthFirstSearchRunnerMessage.StartedWorkMessage startedWorkMessage)
    {
        // Check if sender is known node actor
        if (startedWorkMessage.SenderRef is not BreadthFirstSearchActorRef<TNode, TValue> senderRef || _nodeActorReferences?.Contains(senderRef) != true) return;
        if (!_searchKickOffConfirmed && !senderRef.Equals(_startRef!.Value))
        {
            await _startRef.Value.SendAsync(BreadthFirstSearchMessage.SearchKickedOff(Reference, _kickOffId!.Value));
            _searchKickOffConfirmed = true;
        }

        _pendingWork[(senderRef, startedWorkMessage.TaskId)] = startedWorkMessage;
    }

    private async Task HandleFinishedWorkMessageAsync(BreadthFirstSearchRunnerMessage.FinishedWorkMessage finishedWorkMessage)
    {
        // Check if sender is known node actor
        if (finishedWorkMessage.SenderRef is not BreadthFirstSearchActorRef<TNode, TValue> senderRef || _nodeActorReferences?.Contains(senderRef) != true) return;

        // When there's no more pending work, prepare to finish the run
        if (_pendingWork.Remove((senderRef, finishedWorkMessage.TaskId), out _) && _pendingWork.Count == 0 && _workInitiated)
        {
            // Set expected number of responses
            _pendingWeightResponses = _nodeActorReferences.Count;

            // Ask each node actor to send back its shortest path weight from the start node
            foreach (BreadthFirstSearchActorRef<TNode, TValue> actorRef in _nodeActorReferences)
            {
                await actorRef.SendAsync(BreadthFirstSearchMessage.GetTotalWeightFromStart(Reference));
            }
        }
    }

    private async Task HandleNoNeighboursMessageAsync(BreadthFirstSearchRunnerMessage.NoNeighboursMessage<TValue> noNeighboursMessage)
    {
        // Check if sender is known node actor
        if (noNeighboursMessage.SenderRef is not BreadthFirstSearchActorRef<TNode, TValue> senderRef || _nodeActorReferences?.Contains(senderRef) != true) return;

        if (_workInitiated)
        {
            BreadthFirstSearchRunnerMessage.RunFinishedImmediatelyMessage<TValue> runFinishedImmediatelyMessage =
                BreadthFirstSearchRunnerMessage.RunFinishedImmediately(Reference, noNeighboursMessage.ActorNodeValue);
            await Reference.SendAsync(runFinishedImmediatelyMessage);
        }
    }

    private Task HandleRunFinishedMessageAsync(BreadthFirstSearchRunnerMessage.RunFinishedMessage runFinishedMessage)
    {
        // Check if message comes from self
        if (runFinishedMessage.SenderRef is not BreadthFirstSearchRunnerActorRef selfRef) return Task.CompletedTask;
        if (!selfRef.Equals(Reference)) return Task.CompletedTask;

        // Internally signal run completion
        _runCompletionSource?.TrySetResult(true);
        return Task.CompletedTask;
    }

    private Task HandleRunFinishedImmediatelyMessageAsync(BreadthFirstSearchRunnerMessage.RunFinishedImmediatelyMessage<TValue> runFinishedImmediatelyMessage)
    {
        // Check if message comes from self
        if (runFinishedImmediatelyMessage.SenderRef is not BreadthFirstSearchRunnerActorRef selfRef) return Task.CompletedTask;
        if (!selfRef.Equals(Reference)) return Task.CompletedTask;

        // Update shortest-path distances
        _breadthFirstSearchDistances.TryAdd(runFinishedImmediatelyMessage.StartValue, TWeight.AdditiveIdentity);

        // Internally signal run completion
        _runCompletionSource?.TrySetResult(true);
        return Task.CompletedTask;
    }

    protected override async ValueTask DisposeActorAsync()
    {
        Task[] disposeActorTasks = _nodeActors?
            .Select(actor => actor.DisposeAsync().AsTask())
            .ToArray() ?? [];
        await Task.WhenAll(disposeActorTasks);
        _nodeActors = null;
        _nodeActorReferences = null;

        _runCompletionSource = null;
        _startRef = null;
        _workInitiated = false;
        _searchKickOffConfirmed = false;
        _kickOffId = null;
    }
}
