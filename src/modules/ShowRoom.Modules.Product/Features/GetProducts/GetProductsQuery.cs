namespace ShowRoom.Modules.Product.Features.GetProducts;

/// <summary>Query: list products, paginated and optionally filtered by a name search term.</summary>
public sealed record GetProductsQuery(int Page, int PageSize, string? Search);
