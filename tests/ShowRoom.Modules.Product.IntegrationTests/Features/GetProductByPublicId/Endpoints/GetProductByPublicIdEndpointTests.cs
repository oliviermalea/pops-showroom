namespace ShowRoom.Modules.Product.IntegrationTests.Features.GetProductByPublicId.Endpoints;

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Product;
using ShowRoom.Modules.Product.Features.CreateProduct;
using ShowRoom.Modules.Product.Features.GetProductByPublicId;
using ShowRoom.Testing.Configuration;
using ShowRoom.Testing.Database;

public class GetProductByPublicIdEndpointTests(
    ProductBusinessWebFactory applicationInMemoryFactory,
    DatabaseContainer database)
    : IClassFixture<ProductBusinessWebFactory>,
      IClassFixture<DatabaseContainer>
{
    private static readonly string ProductsRoute = $"/api/v1/{ProductModule.RouteSegment}";

    private WebApplicationFactory<Program> ConfiguredFactory =>
        applicationInMemoryFactory
            .WithContainerDatabaseConfigured(new ProductDatabaseConfiguration(database.ConnectionString!));

    [Fact]
    public async Task Should_Get_Product_By_PublicId()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var client = ConfiguredFactory.CreateClient();
        var command = new CreateProductCommand
        {
            Name = "Surf des mers",
            Description = "Une planche légendaire",
            Price = 349.90m,
            Currency = "EUR",
        };

        var createResponse = await client.PostAsJsonAsync(ProductsRoute, command, cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var publicId = await createResponse.Content.ReadFromJsonAsync<string>(cancellationToken);

        // Act
        HttpResponseMessage sut = await client.GetAsync($"{ProductsRoute}/{publicId}", cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await sut.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken);
        body.Should().NotBeNull();
        body!.PublicId.Value.Should().Be(publicId);
        body.Name.Should().Be("Surf des mers");
        body.Price.Should().Be(349.90m);
        body.Currency.Should().Be("EUR");
        body.Status.Should().Be("Available");
    }

    [Fact]
    public async Task Should_Return_NotFound_For_Unknown_PublicId()
    {
        var unknownPublicId = PublicIdFactory.ForProduct().Value.Value;

        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .GetAsync($"{ProductsRoute}/{unknownPublicId}", CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_Return_BadRequest_For_Malformed_PublicId()
    {
        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .GetAsync($"{ProductsRoute}/not-a-valid-public-id", CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
