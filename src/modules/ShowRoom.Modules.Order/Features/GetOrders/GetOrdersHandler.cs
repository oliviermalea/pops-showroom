using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Application.Pagination;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.Modules.Order.Observability;
using ShowRoom.Modules.Order.Persistence;
using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Features.GetOrders;

internal sealed class GetOrdersHandler(
    OrdersContext context,
    ILogger<GetOrdersHandler> logger)
    : IQueryHandler<GetOrdersQuery, Result<GetOrdersResponse>>
{
    private const string FeatureName = "GetOrders";
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<GetOrdersResponse>> HandleAsync(
        GetOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(OrderConventions.ModuleName, FeatureName, requestId);
        using var activity = OrderTelemetry.ActivitySource.StartActivity("order.get_orders");

        var (page, pageSize) = NormalizePagination(query.Page, query.PageSize);

        activity?
            .SetCommonTags(OrderConventions.ModuleName, FeatureName, requestId)
            .SetTag("order.page", page)
            .SetTag("order.page_size", pageSize)
            .SetTag("order.has_customer_filter", !string.IsNullOrWhiteSpace(query.CustomerPublicId));

        var ordersQuery = context.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.CustomerPublicId))
        {
            if (!PublicId.TryParse(query.CustomerPublicId, out var customerPublicId))
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Malformed customer public id filter");
                logger.LogWarning("Rejecting order listing with malformed customer public id filter");
                return Error.Validation(
                    "Order.InvalidCustomerFilter",
                    "The customer public id filter is not a valid public id.");
            }

            ordersQuery = ordersQuery.Where(order => order.CustomerPublicId == customerPublicId!);
        }

        logger.LogInformation(
            "Listing orders page {Page} size {PageSize} (customer filter: {HasFilter})",
            page,
            pageSize,
            !string.IsNullOrWhiteSpace(query.CustomerPublicId));

        var ordered = ordersQuery.OrderByDescending(order => order.CreatedAt);

        var paged = await ordered.ToPagedResultAsync(
            page,
            pageSize,
            GetOrdersAssembler.ToSummary,
            cancellationToken);

        activity?.SetTag("order.result.total_items", paged.TotalItems);
        logger.LogInformation("Retrieved {TotalItems} orders ({ReturnedItems} on page {Page})",
            paged.TotalItems,
            paged.Items.Count,
            paged.Page);

        return Result<GetOrdersResponse>.Success(new GetOrdersResponse(
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
