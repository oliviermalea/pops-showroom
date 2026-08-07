using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Customer.Features.GetCustomers;

/// <summary>A compact customer representation for list views (no phone/updated detail).</summary>
public sealed record CustomerSummaryResponse(
    PublicId PublicId,
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    string Status,
    DateTimeOffset CreatedAt);

/// <summary>Paginated list of customer summaries.</summary>
public sealed record GetCustomersResponse(
    IReadOnlyCollection<CustomerSummaryResponse> Customers,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
