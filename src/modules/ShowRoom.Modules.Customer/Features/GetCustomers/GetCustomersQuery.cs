namespace ShowRoom.Modules.Customer.Features.GetCustomers;

/// <summary>
/// Query: list customers, paginated and optionally filtered by a free-text search over the
/// first/last name.
/// </summary>
public sealed record GetCustomersQuery(int Page, int PageSize, string? Search);
