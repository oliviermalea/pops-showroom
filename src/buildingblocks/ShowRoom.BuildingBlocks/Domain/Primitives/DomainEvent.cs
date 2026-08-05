namespace ShowRoom.BuildingBlocks.Domain.Primitives;

/// <summary>Base class for domain events (internal to a module/domain).</summary>
/// <param name="Id">The unique identifier of the domain event.</param>
public abstract record DomainEvent(Guid Id);
