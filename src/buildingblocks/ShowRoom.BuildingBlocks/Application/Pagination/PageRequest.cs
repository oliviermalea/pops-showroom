namespace ShowRoom.BuildingBlocks.Application.Pagination;

/// <summary>
/// Standard request object for paginated queries.
/// </summary>
public sealed record PageRequest
{
    /// <summary>1-based page number. Defaults to 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Page size. Defaults to 20.</summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Optional maximum page size enforced by the application. If null, no upper bound is applied.
    /// </summary>
    public int? MaxPageSize { get; init; }

    /// <summary>
    /// Returns normalized pagination values (page &gt;= 1, pageSize within bounds).
    /// </summary>
    public (int Page, int PageSize) Normalize()
    {
        var page = Page < 1 ? 1 : Page;

        var pageSize = PageSize < 1 ? 20 : PageSize;

        if (MaxPageSize is { } max && pageSize > max)
            pageSize = max;

        return (page, pageSize);
    }
}
