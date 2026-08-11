using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Customer.Features.GetCustomers;

/// <summary>
/// A compact customer representation for list views (no phone/updated detail). The paginated list is
/// returned as the shared <see cref="ShowRoom.BuildingBlocks.Application.Pagination.PagedResult{T}"/>.
/// </summary>
public sealed record CustomerSummaryResponse(
    PublicId PublicId,
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    string Status,
    DateTimeOffset RegisteredOn);
