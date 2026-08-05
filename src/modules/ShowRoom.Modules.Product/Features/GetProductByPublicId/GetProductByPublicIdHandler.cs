using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.Modules.Product.Domain;
using ShowRoom.Modules.Product.Persistence;

namespace ShowRoom.Modules.Product.Features.GetProductByPublicId;

public sealed class GetProductByPublicIdHandler(
    ProductsContext context,
    ILogger<GetProductByPublicIdHandler> logger)
    : IQueryHandler<GetProductByPublicIdQuery, Result<ProductResponse>>
{
    private const string Feature = nameof(GetProductByPublicIdHandler);

    public async Task<Result<ProductResponse>> HandleAsync(
        GetProductByPublicIdQuery query,
        CancellationToken cancellationToken = default)
    {
        using var scope = logger.BeginModuleScope(ProductConventions.ModuleName, Feature);

        var publicId = query.PublicId;
        logger.LogInformation("Fetching product by public id {PublicId}", publicId.Value);

        var product = await context.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.PublicId == publicId, cancellationToken);

        if (product is null)
        {
            logger.LogWarning("Product {PublicId} not found", publicId.Value);
            return ProductErrors.NotFound(publicId);
        }

        logger.LogInformation("Product {PublicId} retrieved", publicId.Value);
        return Result<ProductResponse>.Success(GetProductByPublicIdAssembler.From(product));
    }
}
