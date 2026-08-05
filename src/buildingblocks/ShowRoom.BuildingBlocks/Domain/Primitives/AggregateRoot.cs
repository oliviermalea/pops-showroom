namespace ShowRoom.BuildingBlocks.Domain.Primitives;

/// <summary>Aggregate root base class with a strongly-typed identifier.</summary>
/// <typeparam name="TId">The type of the aggregate root's unique identifier.</typeparam>
public abstract class AggregateRoot<TId>(TId id) : Entity<TId>(id);

/// <summary>Aggregate root base class whose identifier is declared by the derived type.</summary>
public abstract class AggregateRoot : Entity
{
    protected AggregateRoot()
    {
    }
}
