using ShowRoom.BuildingBlocks.Domain.Primitives;
using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Customer.Domain;

/// <summary>
/// Raised by <see cref="Customer"/> when its phone number is set, changed, or cleared (a <c>null</c>
/// <see cref="NewPhone"/> means the number was removed).
/// </summary>
public sealed record CustomerPhoneChanged(
    Guid Id,
    CustomerId CustomerId,
    PublicId PublicId,
    string? PreviousPhone,
    string? NewPhone) : DomainEvent(Id);
