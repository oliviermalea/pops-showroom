using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Features.GetOrders;

/// <summary>Maps the <see cref="OrderAggregate"/> domain aggregate to its compact list summary.</summary>
public static class GetOrdersAssembler
{
    public static OrderSummaryResponse ToSummary(OrderAggregate order)
        => new(
            PublicId: order.PublicId,
            CustomerPublicId: order.CustomerPublicId.Value,
            Status: order.Status.Value,
            Currency: order.Currency.Value,
            TotalAmount: order.TotalAmount,
            ItemCount: order.Lines.Count,
            CreatedAt: order.CreatedAt);
}
