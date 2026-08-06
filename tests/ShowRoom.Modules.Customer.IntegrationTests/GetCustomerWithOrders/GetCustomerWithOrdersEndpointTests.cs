namespace ShowRoom.Modules.Customer.IntegrationTests.GetCustomerWithOrders;

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Customer;
using ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.Modules.Order.Domain;
using ShowRoom.Modules.Order.Persistence;
using ShowRoom.SharedKernel.Emails;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;
using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

/// <summary>
/// End-to-end proof that GetCustomerWithOrders fetches the order history from the Order module over a
/// real RabbitMQ broker (AMQP request/reply), with both modules live in the same host.
/// </summary>
[Collection("CustomerWithOrders")]
public class GetCustomerWithOrdersEndpointTests(CustomerWithOrdersBusinessWebFactory factory)
{
    private static readonly string CustomersRoute = $"/api/v1/{CustomerConventions.RouteSegment}";

    [Fact]
    public async Task Should_Return_Customer_With_Orders_Fetched_Over_The_Bus()
    {
        // Arrange — seed a customer (Customer DB) and two orders for it (Order DB).
        var cancellationToken = CancellationToken.None;
        var customerPublicId = await SeedCustomerAsync();
        await SeedOrderAsync(customerPublicId, "Surf des mers", quantity: 2, unitPrice: 10m);
        await SeedOrderAsync(customerPublicId, "Kayak du couchant", quantity: 1, unitPrice: 5.5m);

        // Act
        var response = await factory.CreateClient()
            .GetAsync($"{CustomersRoute}/{customerPublicId}/with-orders", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CustomerWithOrdersResponse>(cancellationToken);
        body.Should().NotBeNull();
        body!.PublicId.Value.Should().Be(customerPublicId);
        body.OrdersAvailable.Should().BeTrue("the Order module replied over RabbitMQ");
        body.Orders.Should().HaveCount(2);
        body.Orders.Select(o => o.TotalAmount).Should().Contain([20m, 5.5m]);
    }

    private async Task<string> SeedCustomerAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomersContext>();

        var customer = CustomerAggregate.Create(
            "Grace", "Hopper", Email.Create($"grace.{Guid.NewGuid():N}@example.com").Value, phone: null, DateTimeOffset.UtcNow);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        return customer.PublicId.Value;
    }

    private async Task SeedOrderAsync(string customerPublicId, string productName, int quantity, decimal unitPrice)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersContext>();

        var order = OrderAggregate.Create(
            PublicId.Parse(customerPublicId),
            "EUR",
            [new OrderLineDraft(PublicIdFactory.ForProduct().Value, productName, quantity, unitPrice)],
            DateTimeOffset.UtcNow).Value;
        db.Orders.Add(order);
        await db.SaveChangesAsync();
    }
}
