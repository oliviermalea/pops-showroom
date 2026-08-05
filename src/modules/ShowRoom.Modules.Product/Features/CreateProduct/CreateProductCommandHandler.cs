using FluentValidation;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Application.Validations;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Observability.Logging;
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
        using var scope = logger.BeginModuleScope(ProductConventions.ModuleName, FeatureName);
        logger.LogInformation("Processing product creation");

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            logger.LogWarning("Validation failed for create product request");
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
            logger.LogWarning("Product creation rejected by domain rules");
            return Result<PublicId>.Fail(productResult.Errors);
        }

        var product = productResult.Value;

        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);

        var publicId = CreateProductAssembler.From(product);
        logger.LogInformation("Product created with public id {PublicId}", publicId.Value);

        return Result<PublicId>.Success(publicId);
    }
}
