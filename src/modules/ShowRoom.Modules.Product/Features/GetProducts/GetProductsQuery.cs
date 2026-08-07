namespace ShowRoom.Modules.Product.Features.GetProducts;

/// <summary>Query: list products, paginated and optionally filtered to a single product by its public id.</summary>
public sealed record GetProductsQuery(int Page, int PageSize, string? PublicId);
