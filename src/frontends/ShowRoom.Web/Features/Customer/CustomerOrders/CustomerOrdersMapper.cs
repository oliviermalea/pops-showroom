using ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models;

namespace ShowRoom.Web.Features.Customer.CustomerOrders;

/// <summary>Maps the aggregated API contract to the order-history view model.</summary>
public static class CustomerOrdersMapper
{
    public static CustomerOrdersView FromApi(CustomerWithOrdersResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var orders = (response.Orders ?? [])
            .Select(ToOrder)
            .ToList();

        return new CustomerOrdersView(
            PublicId: response.PublicId,
            DisplayName: CustomerFormat.DisplayName(response.DisplayName, response.FirstName, response.LastName),
            Email: response.Email,
            OrdersAvailable: response.OrdersAvailable,
            Orders: orders);
    }

    private static CustomerOrderView ToOrder(OrderHistoryLineResponse order)
    {
        var lines = (order.Lines ?? [])
            .Select(line => ToLine(line, order.Currency))
            .ToList();

        return new CustomerOrderView(
            PublicId: order.OrderPublicId,
            Status: order.Status,
            OrderDate: CustomerFormat.Date(order.OrderDate),
            TotalAmount: CustomerFormat.Money(order.TotalAmount, order.Currency),
            ItemCount: CustomerFormat.ItemCount(lines.Sum(line => line.Quantity)),
            Lines: lines);
    }

    private static CustomerOrderLineView ToLine(OrderLineDetailResponse line, string currency) => new(
        ProductPublicId: line.ProductPublicId,
        ProductName: line.ProductName,
        Quantity: line.Quantity,
        UnitPrice: CustomerFormat.Money(line.UnitPrice, currency),
        LineTotal: CustomerFormat.Money(line.LineTotal, currency));
}
