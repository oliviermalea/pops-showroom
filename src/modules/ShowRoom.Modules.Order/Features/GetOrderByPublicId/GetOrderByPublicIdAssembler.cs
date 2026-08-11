using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Features.GetOrderByPublicId;

/// <summary>Maps the <see cref="OrderAggregate"/> domain aggregate to its detailed HTTP response.</summary>
public static class GetOrderByPublicIdAssembler
{
    public static OrderResponse From(OrderAggregate order)
        => new(
            PublicId: order.PublicId,
            CustomerPublicId: order.CustomerPublicId.Value,
            Currency: order.Currency.Value,
            Status: order.Status.Value,
            OrderDate: order.CreatedAt,
            TotalAmount: order.TotalAmount,
            Lines: order.Lines
                .Select(line => new OrderLineResponse(
                    ProductPublicId: line.ProductPublicId.Value,
                    ProductName: line.ProductName,
                    Quantity: line.Quantity,
                    UnitPrice: line.UnitPrice,
                    LineTotal: line.LineTotal))
                .ToList());
}
