using Actors;

namespace ActorGraphs.OrchestratedSearch;

public readonly record struct OrchestratedSearchActorId(Guid Id) : IActorId<OrchestratedSearchActorId>
{
    public static OrchestratedSearchActorId New()
        => new(Guid.NewGuid());

    public readonly int CompareTo(OrchestratedSearchActorId other)
        => Id.CompareTo(other.Id);
}
