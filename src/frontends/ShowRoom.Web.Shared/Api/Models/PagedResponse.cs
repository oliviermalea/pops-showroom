namespace ShowRoom.Web.Shared.Api.Models;

/// <summary>
/// Wire shape of the shared pagination envelope returned by every ShowRoom list endpoint
/// (<c>ShowRoom.BuildingBlocks.Application.Pagination.PagedResult&lt;T&gt;</c>). The backend also serialises
/// <c>hasPrevious</c>/<c>hasNext</c>; they are derived from the page numbers here rather than trusted,
/// so the front keeps a single source of truth for the navigation state.
/// </summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
