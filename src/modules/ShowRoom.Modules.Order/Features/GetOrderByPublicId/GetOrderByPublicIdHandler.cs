using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.Modules.Order.Domain;
using ShowRoom.Modules.Order.Persistence;

namespace ShowRoom.Modules.Order.Features.GetOrderByPublicId;

public sealed class GetOrderByPublicIdHandler(
    OrdersContext context,
    ILogger<GetOrderByPublicIdHandler> logger)
    : IQueryHandler<GetOrderByPublicIdQuery, Result<OrderResponse>>
{
    private const string Feature = nameof(GetOrderByPublicIdHandler);

    public async Task<Result<OrderResponse>> HandleAsync(
        GetOrderByPublicIdQuery query,
        CancellationToken cancellationToken = default)
    {
        using var scope = logger.BeginModuleScope(OrderConventions.ModuleName, Feature);

        var publicId = query.PublicId;
        logger.LogInformation("Fetching order by public id {PublicId}", publicId.Value);

        // Owned line collection is loaded together with the aggregate.
        var order = await context.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.PublicId == publicId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order {PublicId} not found", publicId.Value);
            return OrderErrors.NotFound(publicId);
        }

        logger.LogInformation("Order {PublicId} retrieved", publicId.Value);
        return Result<OrderResponse>.Success(GetOrderByPublicIdAssembler.From(order));
    }
}
