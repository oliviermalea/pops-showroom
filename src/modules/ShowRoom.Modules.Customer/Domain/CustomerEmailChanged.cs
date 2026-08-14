using ShowRoom.BuildingBlocks.Domain.Primitives;
using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Customer.Domain;

/// <summary>
/// Raised by the <see cref="Customer"/> aggregate when its email address actually changes (a no-op change
/// raises nothing). Internal domain event: it carries the customer's public id and the previous/new
/// addresses. Dispatch is wired at the persistence boundary — see the handler / SaveChanges pipeline.
/// </summary>
public sealed record CustomerEmailChanged(
    Guid Id,
    CustomerId CustomerId,
    PublicId PublicId,
    string PreviousEmail,
    string NewEmail) : DomainEvent(Id);
