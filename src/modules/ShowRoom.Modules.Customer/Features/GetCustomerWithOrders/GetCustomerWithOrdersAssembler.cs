using ShowRoom.Modules.Order.Contracts.Messaging;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;

/// <summary>
/// Assembles the aggregated customer-with-orders view from the Customer aggregate and the order
/// summaries returned by the Order module over the bus.
/// </summary>
public static class GetCustomerWithOrdersAssembler
{
    public static CustomerWithOrdersResponse From(
        CustomerAggregate customer,
        IReadOnlyCollection<CustomerOrderSummary> orders,
        bool ordersAvailable)
        => new(
            PublicId: customer.PublicId,
            FirstName: customer.FirstName,
            LastName: customer.LastName,
            DisplayName: customer.DisplayName,
            Email: customer.Email.Value,
            Phone: customer.Phone?.Value,
            Status: customer.Status.Value,
            RegisteredOn: customer.CreatedAt,
            OrdersAvailable: ordersAvailable,
            Orders: orders
                .Select(order => new OrderHistoryLine(
                    OrderPublicId: order.OrderPublicId,
                    Status: order.Status,
                    Currency: order.Currency,
                    TotalAmount: order.TotalAmount,
                    ItemCount: order.ItemCount,
                    OrderDate: order.OrderDate))
                .ToList());
}
