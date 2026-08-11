using ProductAggregate = ShowRoom.Modules.Product.Domain.Product;

namespace ShowRoom.Modules.Product.Features.GetProducts;

/// <summary>Maps the <see cref="ProductAggregate"/> domain aggregate to its compact list summary.</summary>
public static class GetProductsAssembler
{
    public static ProductSummaryResponse ToSummary(ProductAggregate product)
        => new(
            PublicId: product.PublicId,
            Name: product.Name,
            Price: product.Price,
            Currency: product.Currency.Value,
            Status: product.Status.Value);
}
