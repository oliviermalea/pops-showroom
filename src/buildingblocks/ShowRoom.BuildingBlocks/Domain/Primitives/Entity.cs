namespace ShowRoom.BuildingBlocks.Domain.Primitives;

/// <summary>Entity base class carrying a strongly-typed identifier and domain/integration events.</summary>
/// <typeparam name="TId">The type of the entity's unique identifier.</typeparam>
public abstract class Entity<TId>
{
    private readonly List<DomainEvent> _domainEvents = [];
    private readonly List<IntegrationEvent> _integrationEvents = [];

    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    public TId Id { get; init; }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public IReadOnlyCollection<IntegrationEvent> IntegrationEvents => _integrationEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public void ClearIntegrationEvents() => _integrationEvents.Clear();

    protected void RaiseDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    protected void RemoveDomainEvent(DomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

    protected void RaiseIntegrationEvent(IntegrationEvent integrationEvent) => _integrationEvents.Add(integrationEvent);

    protected void RemoveIntegrationEvent(IntegrationEvent integrationEvent) => _integrationEvents.Remove(integrationEvent);
}

/// <summary>Entity base class (identifier declared by the derived type).</summary>
public abstract class Entity
{
    private readonly List<DomainEvent> _domainEvents = [];
    private readonly List<IntegrationEvent> _integrationEvents = [];

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public IReadOnlyCollection<IntegrationEvent> IntegrationEvents => _integrationEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public void ClearIntegrationEvents() => _integrationEvents.Clear();

    protected void RaiseDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    protected void RemoveDomainEvent(DomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

    protected void RaiseIntegrationEvent(IntegrationEvent integrationEvent) => _integrationEvents.Add(integrationEvent);

    protected void RemoveIntegrationEvent(IntegrationEvent integrationEvent) => _integrationEvents.Remove(integrationEvent);
}
