using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Product.Features.GetProductByPublicId;

/// <summary>Query: fetch the detailed view of a product by its public identifier.</summary>
public sealed record GetProductByPublicIdQuery(PublicId PublicId);
