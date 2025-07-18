using Actors;

namespace ActorGraphs.OrchestratedSearch;

public class OrchestratedSearchRunnerActorRefFactory : ActorRefFactory<OrchestratedSearchRunnerId, OrchestratedSearchRunnerMessage, OrchestratedSearchRunnerActorRef>
{
    public OrchestratedSearchRunnerActorRefFactory(OrchestratedSearchRunnerId id) : base(id)
    {
    }

    public override OrchestratedSearchRunnerActorRef Create(Func<OrchestratedSearchRunnerMessage, ValueTask> send)
        => new(Id, send);
}