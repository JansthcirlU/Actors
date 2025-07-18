using Actors;

namespace ActorGraphs.OrchestratedSearch;

public readonly record struct OrchestratedSearchRunnerId(Guid Id) : IActorId<OrchestratedSearchRunnerId>
{
    public static OrchestratedSearchRunnerId New()
        => new(Guid.NewGuid());

    public int CompareTo(OrchestratedSearchRunnerId other)
        => Id.CompareTo(other.Id);
}
