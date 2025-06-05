using Actors;

namespace ActorGraphs;

public readonly record struct BreadthFirstSearchRunnerId(Guid Id) : IActorId<BreadthFirstSearchRunnerId>
{
    public static BreadthFirstSearchRunnerId New()
        => new(Guid.NewGuid());

    public int CompareTo(BreadthFirstSearchRunnerId other)
        => Id.CompareTo(other.Id);
}
