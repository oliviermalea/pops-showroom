using System.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Application.Validations;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.BuildingBlocks.Time;
using ShowRoom.Modules.Product.Persistence;
using ProductAggregate = ShowRoom.Modules.Product.Domain.Product;

namespace ShowRoom.Modules.Product.Features.CreateProduct;

internal sealed class CreateProductCommandHandler(
    ProductsContext context,
    IDateTimeProvider dateTimeProvider,
    IValidator<CreateProductCommand> validator,
    ILogger<CreateProductCommandHandler> logger)
    : ICommandHandler<CreateProductCommand, Result<PublicId>>
{
    private const string FeatureName = "CreateProduct";

    public async Task<Result<PublicId>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(ProductModule.ModuleName, FeatureName, requestId);
        using var activity = ProductModule.ActivitySource.StartActivity("product.create_product");

        activity?
            .SetCommonTags(ProductModule.ModuleName, FeatureName, requestId)
            .SetTag("product.name", command.Name)
            .SetTag("product.price", command.Price);

        logger.LogInformation("Processing product creation");

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
            logger.LogWarning("Validation failed for create product request");
            CreateProductMetrics.RecordRejected("validation");
            return Result<PublicId>.Fail(validation.ToErrors());
        }

        var productResult = ProductAggregate.Create(
            command.Name,
            command.Description,
            command.Price,
            command.Currency,
            dateTimeProvider.UtcNow);

        if (productResult.IsFailure)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Domain rules rejected the product");
            logger.LogWarning("Product creation rejected by domain rules");
            CreateProductMetrics.RecordRejected("domain");
            return Result<PublicId>.Fail(productResult.Errors);
        }

        var product = productResult.Value;

        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);

        var publicId = CreateProductAssembler.From(product);
        activity?.SetTag("product.public_id", publicId.Value);
        logger.LogInformation("Product created with public id {PublicId}", publicId.Value);

        return Result<PublicId>.Success(publicId);
    }
}
