namespace ShowRoom.Web.Client.Infrastructure.Api.Refit.Catalog.Models;

/// <summary>
/// Wire contract of one row of <c>GET /api/v1/products</c>. Mirrors the backend
/// <c>ProductSummaryResponse</c>: the product is identified by its public id only.
/// </summary>
public sealed record ProductSummaryResponse(
    string PublicId,
    string Name,
    decimal Price,
    string Currency,
    string Status);
