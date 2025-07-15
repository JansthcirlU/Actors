using Actors;

namespace ActorGraphs;

public class BreadthFirstSearchRunnerActorRefFactory : ActorRefFactory<BreadthFirstSearchRunnerId, BreadthFirstSearchRunnerMessage, BreadthFirstSearchRunnerActorRef>
{
    public BreadthFirstSearchRunnerActorRefFactory(BreadthFirstSearchRunnerId id) : base(id)
    {
    }

    public override BreadthFirstSearchRunnerActorRef Create(Func<BreadthFirstSearchRunnerMessage, ValueTask> send)
        => new(Id, send);
}