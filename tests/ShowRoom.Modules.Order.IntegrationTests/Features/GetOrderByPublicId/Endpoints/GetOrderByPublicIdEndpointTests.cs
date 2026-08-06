namespace ShowRoom.Modules.Order.IntegrationTests.Features.GetOrderByPublicId.Endpoints;

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Order;
using ShowRoom.Modules.Order.Features.CreateOrder;
using ShowRoom.Modules.Order.Features.GetOrderByPublicId;
using ShowRoom.Testing.Configuration;
using ShowRoom.Testing.Database;

public class GetOrderByPublicIdEndpointTests(
    OrderBusinessWebFactory applicationInMemoryFactory,
    DatabaseContainer database)
    : IClassFixture<OrderBusinessWebFactory>,
      IClassFixture<DatabaseContainer>
{
    private static readonly string OrdersRoute = $"/api/v1/{OrderModule.RouteSegment}";

    private WebApplicationFactory<Program> ConfiguredFactory =>
        applicationInMemoryFactory
            .WithContainerDatabaseConfigured(new OrderDatabaseConfiguration(database.ConnectionString!));

    [Fact]
    public async Task Should_Get_Order_With_Lines_And_Total_By_PublicId()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var client = ConfiguredFactory.CreateClient();
        var command = new CreateOrderCommand
        {
            CustomerPublicId = PublicIdFactory.ForCustomer().Value.Value,
            Currency = "EUR",
            Lines =
            [
                new CreateOrderLine { ProductPublicId = PublicIdFactory.ForContentNode().Value.Value, ProductName = "Widget", Quantity = 2, UnitPrice = 10m },
                new CreateOrderLine { ProductPublicId = PublicIdFactory.ForContentNode().Value.Value, ProductName = "Gadget", Quantity = 1, UnitPrice = 5.5m },
            ],
        };

        var createResponse = await client.PostAsJsonAsync(OrdersRoute, command, cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var publicId = await createResponse.Content.ReadFromJsonAsync<string>(cancellationToken);

        // Act
        HttpResponseMessage sut = await client.GetAsync($"{OrdersRoute}/{publicId}", cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await sut.Content.ReadFromJsonAsync<OrderResponse>(cancellationToken);
        body.Should().NotBeNull();
        body!.PublicId.Value.Should().Be(publicId);
        body.CustomerPublicId.Should().Be(command.CustomerPublicId);
        body.Currency.Should().Be("EUR");
        body.Status.Should().Be("Pending");
        body.TotalAmount.Should().Be(25.5m);
        body.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_Return_NotFound_For_Unknown_PublicId()
    {
        var unknownPublicId = PublicIdFactory.ForOrder().Value.Value;

        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .GetAsync($"{OrdersRoute}/{unknownPublicId}", CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_Return_BadRequest_For_Malformed_PublicId()
    {
        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .GetAsync($"{OrdersRoute}/not-a-valid-public-id", CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
