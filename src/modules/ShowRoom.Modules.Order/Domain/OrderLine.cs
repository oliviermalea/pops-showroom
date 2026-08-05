using ShowRoom.BuildingBlocks.Domain.Primitives;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Order.Domain;

/// <summary>
/// A line inside the <see cref="Order"/> aggregate. Owned by the aggregate (no independent identity
/// over HTTP). References the product by <see cref="PublicId"/> and snapshots its name and unit price
/// at order time, so the order stays valid even if the product later changes in its own module.
/// </summary>
public sealed class OrderLine : Entity<OrderLineId>
{
    private OrderLine(
        OrderLineId id,
        PublicId productPublicId,
        string productName,
        int quantity,
        decimal unitPrice)
        : base(id)
    {
        ProductPublicId = productPublicId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public PublicId ProductPublicId { get; private set; }

    public string ProductName { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    /// <summary>Validates and builds a line; returns an error result rather than throwing.</summary>
    internal static Result<OrderLine> Create(
        PublicId productPublicId,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        if (productPublicId is null)
        {
            return OrderErrors.ProductRequired;
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            return OrderErrors.ProductNameRequired;
        }

        if (quantity <= 0)
        {
            return OrderErrors.InvalidQuantity;
        }

        if (unitPrice < 0)
        {
            return OrderErrors.InvalidUnitPrice;
        }

        return Result<OrderLine>.Success(new OrderLine(
            OrderLineId.FromGuid(Guid.CreateVersion7()),
            productPublicId,
            productName.Trim(),
            quantity,
            unitPrice));
    }

    /// <summary>Rehydrates a line from already-persisted state (used by EF).</summary>
    internal static OrderLine Restore(
        OrderLineId id,
        PublicId productPublicId,
        string productName,
        int quantity,
        decimal unitPrice)
        => new(id, productPublicId, productName, quantity, unitPrice);
}
