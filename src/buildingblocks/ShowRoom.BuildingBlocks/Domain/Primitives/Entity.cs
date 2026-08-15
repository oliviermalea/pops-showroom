namespace ShowRoom.BuildingBlocks.Domain.Primitives;

/// <summary>Entity base class carrying a strongly-typed identifier and its domain events.</summary>
/// <typeparam name="TId">The type of the entity's unique identifier.</typeparam>
public abstract class Entity<TId> : IHasDomainEvents
{
    private readonly List<DomainEvent> _domainEvents = [];

    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    public TId Id { get; init; }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    protected void RemoveDomainEvent(DomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
}

/// <summary>Entity base class (identifier declared by the derived type).</summary>
public abstract class Entity : IHasDomainEvents
{
    private readonly List<DomainEvent> _domainEvents = [];

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    protected void RemoveDomainEvent(DomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
}
