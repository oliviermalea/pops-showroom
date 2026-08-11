using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Product.Features.GetProductByPublicId;

/// <summary>
/// Detailed product representation returned over HTTP. Exposes the strongly-typed <see cref="PublicId"/>
/// only; the internal technical identifier is never serialised.
/// </summary>
public sealed record ProductResponse(
    PublicId PublicId,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string Status);
