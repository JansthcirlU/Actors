namespace Actors;

// Marker interface to let you pattern match on more specific ID types
public interface IActorId;
public interface IActorId<TSelf> : IActorId, IComparable<TSelf>, IEquatable<TSelf>
    where TSelf : notnull, IActorId<TSelf>
{

}
