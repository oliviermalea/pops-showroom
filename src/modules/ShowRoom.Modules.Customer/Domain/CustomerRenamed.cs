using ShowRoom.BuildingBlocks.Domain.Primitives;
using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Customer.Domain;

/// <summary>Raised by <see cref="Customer"/> when its name actually changes.</summary>
public sealed record CustomerRenamed(
    Guid Id,
    CustomerId CustomerId,
    PublicId PublicId,
    string FirstName,
    string LastName) : DomainEvent(Id);
