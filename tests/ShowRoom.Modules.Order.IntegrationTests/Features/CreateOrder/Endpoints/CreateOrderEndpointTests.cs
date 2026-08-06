namespace ShowRoom.Modules.Order.IntegrationTests.Features.CreateOrder.Endpoints;

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Order;
using ShowRoom.Modules.Order.Features.CreateOrder;
using ShowRoom.Testing.Configuration;
using ShowRoom.Testing.Database;
using ShowRoom.Testing.Http;

public class CreateOrderEndpointTests(
    OrderBusinessWebFactory applicationInMemoryFactory,
    DatabaseContainer database)
    : IClassFixture<OrderBusinessWebFactory>,
      IClassFixture<DatabaseContainer>
{
    private static readonly string OrdersRoute = $"/api/v1/{OrderModule.RouteSegment}";

    private WebApplicationFactory<Program> ConfiguredFactory =>
        applicationInMemoryFactory
            .WithContainerDatabaseConfigured(new OrderDatabaseConfiguration(database.ConnectionString!));

    private static CreateOrderCommand NewOrderCommand(string? customerPublicId = null) => new()
    {
        CustomerPublicId = customerPublicId ?? PublicIdFactory.ForCustomer().Value.Value,
        Currency = "EUR",
        Lines =
        [
            new CreateOrderLine
            {
                ProductPublicId = PublicIdFactory.ForContentNode().Value.Value,
                ProductName = "Widget",
                Quantity = 2,
                UnitPrice = 9.99m,
            },
        ],
    };

    [Fact]
    public async Task Should_Create_Order_And_Return_PublicId()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var command = NewOrderCommand();

        // Act
        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .PostAsJsonAsync(OrdersRoute, command, cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.Created);
        sut.Headers.Location.Should().NotBeNull();

        var responsePublicId = await sut.Content.ReadFromJsonAsync<string>(cancellationToken);
        responsePublicId.Should().NotBeNullOrWhiteSpace();
        responsePublicId!.Should().StartWith("ord_");

        sut.Headers.Location!.ToString().Should().Contain($"{OrdersRoute}/{responsePublicId}");
        sut.GetIdFromLocationHeader().Should().Be(responsePublicId);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Order_Has_No_Lines()
    {
        var command = NewOrderCommand() with { Lines = [] };

        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .PostAsJsonAsync(OrdersRoute, command, CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Customer_PublicId_Is_Malformed()
    {
        var command = NewOrderCommand(customerPublicId: "not-a-public-id");

        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .PostAsJsonAsync(OrdersRoute, command, CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
