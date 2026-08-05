namespace ShowRoom.BuildingBlocks.Domain.Primitives;

/// <summary>Base class for integration events (external / module-to-module contracts).</summary>
/// <param name="Id">The unique identifier of the integration event.</param>
public abstract record IntegrationEvent(Guid Id);
