using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.Modules.Order.Domain;
using ShowRoom.Modules.Order.Persistence;

namespace ShowRoom.Modules.Order.Features.GetOrderByPublicId;

public sealed class GetOrderByPublicIdHandler(
    OrdersContext context,
    ILogger<GetOrderByPublicIdHandler> logger)
    : IQueryHandler<GetOrderByPublicIdQuery, Result<OrderResponse>>
{
    private const string FeatureName = "GetOrderByPublicId";

    public async Task<Result<OrderResponse>> HandleAsync(
        GetOrderByPublicIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(OrderModule.ModuleName, FeatureName, requestId);
        using var activity = OrderModule.ActivitySource.StartActivity("order.get_order_by_public_id");

        var publicId = query.PublicId;
        activity?
            .SetCommonTags(OrderModule.ModuleName, FeatureName, requestId)
            .SetTag("order.public_id", publicId.Value);

        logger.LogInformation("Fetching order by public id {PublicId}", publicId.Value);

        // Owned line collection is loaded together with the aggregate.
        var order = await context.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.PublicId == publicId, cancellationToken);

        if (order is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Order not found");
            logger.LogWarning("Order {PublicId} not found", publicId.Value);
            return OrderErrors.NotFound(publicId);
        }

        logger.LogInformation("Order {PublicId} retrieved", publicId.Value);
        return Result<OrderResponse>.Success(GetOrderByPublicIdAssembler.From(order));
    }
}
