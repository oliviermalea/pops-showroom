using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ProductAggregate = ShowRoom.Modules.Product.Domain.Product;

namespace ShowRoom.Modules.Product.Features.CreateProduct;

/// <summary>Maps a freshly created <see cref="ProductAggregate"/> to its public identifier.</summary>
internal static class CreateProductAssembler
{
    internal static PublicId From(ProductAggregate product) => product.PublicId;
}
