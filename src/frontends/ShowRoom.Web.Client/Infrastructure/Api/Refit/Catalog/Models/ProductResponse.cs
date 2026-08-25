namespace ShowRoom.Web.Client.Infrastructure.Api.Refit.Catalog.Models;

/// <summary>Wire contract of <c>GET /api/v1/products/{publicId}</c>.</summary>
public sealed record ProductResponse(
    string PublicId,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string Status);
