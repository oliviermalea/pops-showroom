using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Features.GetOrderByPublicId;

/// <summary>Maps the <see cref="OrderAggregate"/> domain aggregate to its detailed HTTP response.</summary>
public static class GetOrderByPublicIdAssembler
{
    public static OrderResponse From(OrderAggregate order)
        => new(
            PublicId: order.PublicId,
            CustomerPublicId: order.CustomerPublicId.Value,
            Currency: order.Currency,
            Status: order.Status.Value,
            TotalAmount: order.TotalAmount,
            Lines: order.Lines
                .Select(line => new OrderLineResponse(
                    ProductPublicId: line.ProductPublicId.Value,
                    ProductName: line.ProductName,
                    Quantity: line.Quantity,
                    UnitPrice: line.UnitPrice,
                    LineTotal: line.LineTotal))
                .ToList(),
            CreatedAt: order.CreatedAt,
            UpdatedAt: order.UpdatedAt);
}
