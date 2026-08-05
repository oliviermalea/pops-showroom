using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Customer.Features.GetCustomerByPublicId;

/// <summary>
/// Detailed customer representation returned over HTTP. Exposes the strongly-typed
/// <see cref="PublicId"/> only (serialised as a GUID string); the internal identifier is never
/// serialised.
/// </summary>
public sealed record CustomerResponse(
    PublicId PublicId,
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    string? Phone,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
