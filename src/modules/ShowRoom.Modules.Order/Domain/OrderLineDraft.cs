using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Order.Domain;

/// <summary>
/// Input specification for a single order line, handed to <see cref="Order.Create"/>. The product is
/// referenced by its <see cref="PublicId"/> only (no cross-module database access); the product name
/// is captured as a snapshot at order time.
/// </summary>
public sealed record OrderLineDraft(
    PublicId ProductPublicId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
