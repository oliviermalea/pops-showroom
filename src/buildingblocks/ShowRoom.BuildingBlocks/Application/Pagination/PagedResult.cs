namespace ShowRoom.BuildingBlocks.Application.Pagination;

/// <summary>
/// Standard paginated result wrapper.
/// </summary>
/// <typeparam name="T">The type of the items on the page.</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>Items for the current page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>1-based page number.</summary>
    public int Page { get; init; }

    /// <summary>Page size used for this result.</summary>
    public int PageSize { get; init; }

    /// <summary>Total number of items across all pages.</summary>
    public int TotalItems { get; init; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages { get; init; }

    /// <summary>Whether there is a previous page.</summary>
    public bool HasPrevious => Page > 1;

    /// <summary>Whether there is a next page.</summary>
    public bool HasNext => Page < TotalPages;

    public PagedResult() { }

    public static PagedResult<T> Empty(int page, int pageSize) =>
        new()
        {
            Items = [],
            Page = page,
            PageSize = pageSize,
            TotalItems = 0,
            TotalPages = 0,
        };

    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalItems)
    {
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
        };
    }
}
