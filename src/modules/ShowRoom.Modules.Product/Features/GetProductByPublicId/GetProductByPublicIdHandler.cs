using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.Modules.Product.Domain;
using ShowRoom.Modules.Product.Observability;
using ShowRoom.Modules.Product.Persistence;

namespace ShowRoom.Modules.Product.Features.GetProductByPublicId;

public sealed class GetProductByPublicIdHandler(
    ProductsContext context,
    ILogger<GetProductByPublicIdHandler> logger)
    : IQueryHandler<GetProductByPublicIdQuery, Result<ProductResponse>>
{
    private const string FeatureName = "GetProductByPublicId";

    public async Task<Result<ProductResponse>> HandleAsync(
        GetProductByPublicIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(ProductConventions.ModuleName, FeatureName, requestId);
        using var activity = ProductTelemetry.ActivitySource.StartActivity("product.get_product_by_public_id");

        var publicId = query.PublicId;
        activity?
            .SetCommonTags(ProductConventions.ModuleName, FeatureName, requestId)
            .SetTag("product.public_id", publicId.Value);

        logger.LogInformation("Fetching product by public id {PublicId}", publicId.Value);

        var product = await context.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.PublicId == publicId, cancellationToken);

        if (product is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Product not found");
            logger.LogWarning("Product {PublicId} not found", publicId.Value);
            return ProductErrors.NotFound(publicId);
        }

        logger.LogInformation("Product {PublicId} retrieved", publicId.Value);
        return Result<ProductResponse>.Success(GetProductByPublicIdAssembler.From(product));
    }
}
