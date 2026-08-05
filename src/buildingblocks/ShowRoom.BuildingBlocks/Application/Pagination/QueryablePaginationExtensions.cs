using Microsoft.EntityFrameworkCore;

namespace ShowRoom.BuildingBlocks.Application.Pagination;

/// <summary>
/// EF Core pagination helpers for queries.
/// </summary>
public static class QueryablePaginationExtensions
{
    /// <summary>Applies pagination to a queryable. Page is 1-based.</summary>
    public static IQueryable<T> ApplyPaging<T>(
        this IQueryable<T> query,
        int page,
        int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;

        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>Executes a paginated query and returns a <see cref="PagedResult{T}"/>.</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .ApplyPaging(page, pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<T>.Create(items, page, pageSize, totalItems);
    }

    /// <summary>
    /// Executes a paginated query and projects each materialized item (projection runs in memory,
    /// so it may safely touch value objects / computed properties that do not translate to SQL).
    /// </summary>
    public static async Task<PagedResult<TResult>> ToPagedResultAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        int page,
        int pageSize,
        Func<TSource, TResult> projection,
        CancellationToken cancellationToken = default)
    {
        var totalItems = await query.CountAsync(cancellationToken);

        var sourceItems = await query
            .ApplyPaging(page, pageSize)
            .ToListAsync(cancellationToken);

        var projected = sourceItems.Select(projection).ToList();

        return PagedResult<TResult>.Create(projected, page, pageSize, totalItems);
    }
}
