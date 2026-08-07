using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Application.Pagination;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.Modules.Customer.Persistence;

namespace ShowRoom.Modules.Customer.Features.GetCustomers;

internal sealed class GetCustomersHandler(
    CustomersContext context,
    ILogger<GetCustomersHandler> logger)
    : IQueryHandler<GetCustomersQuery, Result<GetCustomersResponse>>
{
    private const string FeatureName = "GetCustomers";
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<GetCustomersResponse>> HandleAsync(
        GetCustomersQuery query,
        CancellationToken cancellationToken = default)
    {
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(CustomerModule.ModuleName, FeatureName, requestId);
        using var activity = CustomerModule.ActivitySource.StartActivity("customer.get_customers");

        var (page, pageSize) = NormalizePagination(query.Page, query.PageSize);

        activity?
            .SetCommonTags(CustomerModule.ModuleName, FeatureName, requestId)
            .SetTag("customer.page", page)
            .SetTag("customer.page_size", pageSize)
            .SetTag("customer.has_search", !string.IsNullOrWhiteSpace(query.Search));

        var customersQuery = context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            customersQuery = customersQuery.Where(customer =>
                EF.Functions.ILike(customer.FirstName, pattern)
                || EF.Functions.ILike(customer.LastName, pattern));
        }

        logger.LogInformation(
            "Listing customers page {Page} size {PageSize} (search: {HasSearch})",
            page,
            pageSize,
            !string.IsNullOrWhiteSpace(query.Search));

        var ordered = customersQuery.OrderByDescending(customer => customer.CreatedAt);

        var paged = await ordered.ToPagedResultAsync(
            page,
            pageSize,
            GetCustomersAssembler.ToSummary,
            cancellationToken);

        activity?.SetTag("customer.result.total_items", paged.TotalItems);
        logger.LogInformation("Retrieved {TotalItems} customers ({ReturnedItems} on page {Page})",
            paged.TotalItems,
            paged.Items.Count,
            paged.Page);

        return Result<GetCustomersResponse>.Success(new GetCustomersResponse(
            paged.Items,
            paged.Page,
            paged.PageSize,
            paged.TotalItems,
            paged.TotalPages));
    }

    private static (int Page, int PageSize) NormalizePagination(int page, int pageSize)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        return (normalizedPage, normalizedPageSize);
    }
}
