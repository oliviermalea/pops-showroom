using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Product.Features.GetProducts;

/// <summary>A compact product representation for list views.</summary>
public sealed record ProductSummaryResponse(
    PublicId PublicId,
    string Name,
    decimal Price,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt);

/// <summary>Paginated list of product summaries.</summary>
public sealed record GetProductsResponse(
    IReadOnlyCollection<ProductSummaryResponse> Products,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
