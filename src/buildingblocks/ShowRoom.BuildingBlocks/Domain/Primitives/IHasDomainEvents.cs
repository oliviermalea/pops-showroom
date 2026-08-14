namespace ShowRoom.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Implemented by aggregates/entities that record domain events, so the persistence-layer dispatch
/// interceptor can collect and clear them from the change tracker without knowing the concrete type.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
