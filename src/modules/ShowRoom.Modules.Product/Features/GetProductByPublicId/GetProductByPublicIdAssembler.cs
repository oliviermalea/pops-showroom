using ProductAggregate = ShowRoom.Modules.Product.Domain.Product;

namespace ShowRoom.Modules.Product.Features.GetProductByPublicId;

/// <summary>Maps the <see cref="ProductAggregate"/> domain aggregate to its HTTP <see cref="ProductResponse"/>.</summary>
public static class GetProductByPublicIdAssembler
{
    public static ProductResponse From(ProductAggregate product)
        => new(
            PublicId: product.PublicId,
            Name: product.Name,
            Description: product.Description,
            Price: product.Price,
            Currency: product.Currency.Code,
            Status: product.Status.Value);
}
