using ShowRoom.Modules.Order.Contracts.Messaging;
using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Features.Messaging;

/// <summary>
/// Maps Order aggregates to the cross-module <see cref="OrdersForCustomerResponse"/> reply. Only public
/// ids and denormalised values cross the boundary — no domain types leak.
/// </summary>
public static class GetOrdersForCustomerAssembler
{
    public static OrdersForCustomerResponse From(IReadOnlyCollection<OrderAggregate> orders)
        => new(orders
            .Select(order => new CustomerOrderSummary(
                OrderPublicId: order.PublicId.Value,
                Status: order.Status.Value,
                Currency: order.Currency,
                TotalAmount: order.TotalAmount,
                ItemCount: order.Lines.Count,
                CreatedAt: order.CreatedAt))
            .ToList());
}
