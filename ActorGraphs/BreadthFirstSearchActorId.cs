using Actors;

namespace ActorGraphs;

public readonly record struct BreadthFirstSearchActorId(Guid Id) : IActorId<BreadthFirstSearchActorId>
{
    public static BreadthFirstSearchActorId New()
        => new(Guid.NewGuid());

    public readonly int CompareTo(BreadthFirstSearchActorId other)
        => Id.CompareTo(other.Id);
}
