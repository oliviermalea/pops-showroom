using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Product.Features.GetProducts;

/// <summary>
/// A compact product representation for list views. The paginated list is returned as the shared
/// <see cref="ShowRoom.BuildingBlocks.Application.Pagination.PagedResult{T}"/>.
/// </summary>
public sealed record ProductSummaryResponse(
    PublicId PublicId,
    string Name,
    decimal Price,
    string Currency,
    string Status);
