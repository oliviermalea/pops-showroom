namespace ShowRoom.Modules.Order.IntegrationTests.Features.GetOrders.Endpoints;

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Order;
using ShowRoom.Modules.Order.Features.CreateOrder;
using ShowRoom.Modules.Order.Features.GetOrders;
using ShowRoom.Testing.Configuration;
using ShowRoom.Testing.Database;

public class GetOrdersEndpointTests(
    OrderBusinessWebFactory applicationInMemoryFactory,
    DatabaseContainer database)
    : IClassFixture<OrderBusinessWebFactory>,
      IClassFixture<DatabaseContainer>
{
    private static readonly string OrdersRoute = $"/api/v1/{OrderConventions.RouteSegment}";

    private WebApplicationFactory<Program> ConfiguredFactory =>
        applicationInMemoryFactory
            .WithContainerDatabaseConfigured(new OrderDatabaseConfiguration(database.ConnectionString!));

    private static CreateOrderCommand OrderFor(string customerPublicId) => new()
    {
        CustomerPublicId = customerPublicId,
        Currency = "EUR",
        Lines =
        [
            new CreateOrderLine
            {
                ProductPublicId = PublicIdFactory.ForContentNode().Value.Value,
                ProductName = "Widget",
                Quantity = 1,
                UnitPrice = 12m,
            },
        ],
    };

    [Fact]
    public async Task Should_List_Only_The_Orders_Of_The_Filtered_Customer()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var client = ConfiguredFactory.CreateClient();

        var customerA = PublicIdFactory.ForCustomer().Value.Value;
        var customerB = PublicIdFactory.ForCustomer().Value.Value;

        await client.PostAsJsonAsync(OrdersRoute, OrderFor(customerA), cancellationToken);
        await client.PostAsJsonAsync(OrdersRoute, OrderFor(customerA), cancellationToken);
        await client.PostAsJsonAsync(OrdersRoute, OrderFor(customerB), cancellationToken);

        // Act
        HttpResponseMessage sut = await client.GetAsync(
            $"{OrdersRoute}?customerPublicId={customerA}&page=1&pageSize=20",
            cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await sut.Content.ReadFromJsonAsync<GetOrdersResponse>(cancellationToken);
        body.Should().NotBeNull();
        body!.Orders.Should().OnlyContain(order => order.CustomerPublicId == customerA);
        body.Orders.Should().HaveCount(2);
        body.TotalItems.Should().Be(2);
        body.Page.Should().Be(1);
    }

    [Fact]
    public async Task Should_Paginate_Results()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var client = ConfiguredFactory.CreateClient();
        var customer = PublicIdFactory.ForCustomer().Value.Value;

        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync(OrdersRoute, OrderFor(customer), cancellationToken);
        }

        // Act
        HttpResponseMessage sut = await client.GetAsync(
            $"{OrdersRoute}?customerPublicId={customer}&page=1&pageSize=2",
            cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await sut.Content.ReadFromJsonAsync<GetOrdersResponse>(cancellationToken);
        body.Should().NotBeNull();
        body!.PageSize.Should().Be(2);
        body.Orders.Should().HaveCount(2);
        body.TotalItems.Should().Be(3);
        body.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Should_Return_BadRequest_For_Malformed_Customer_Filter()
    {
        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .GetAsync($"{OrdersRoute}?customerPublicId=not-a-public-id", CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
