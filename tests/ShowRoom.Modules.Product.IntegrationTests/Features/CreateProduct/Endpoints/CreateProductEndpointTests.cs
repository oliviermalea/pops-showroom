namespace ShowRoom.Modules.Product.IntegrationTests.Features.CreateProduct.Endpoints;

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ShowRoom.Modules.Product;
using ShowRoom.Modules.Product.Features.CreateProduct;
using ShowRoom.Testing.Configuration;
using ShowRoom.Testing.Database;
using ShowRoom.Testing.Http;

public class CreateProductEndpointTests(
    ProductBusinessWebFactory applicationInMemoryFactory,
    DatabaseContainer database)
    : IClassFixture<ProductBusinessWebFactory>,
      IClassFixture<DatabaseContainer>
{
    private static readonly string ProductsRoute = $"/api/v1/{ProductModule.RouteSegment}";

    private WebApplicationFactory<Program> ConfiguredFactory =>
        applicationInMemoryFactory
            .WithContainerDatabaseConfigured(new ProductDatabaseConfiguration(database.ConnectionString!));

    private static CreateProductCommand NewProductCommand(string? name = null) => new()
    {
        Name = name ?? $"Surf des mers {Guid.NewGuid():N}",
        Description = "Une planche légendaire",
        Price = 349.90m,
        Currency = "EUR",
    };

    [Fact]
    public async Task Should_Create_Product_And_Return_PublicId()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var command = NewProductCommand();

        // Act
        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .PostAsJsonAsync(ProductsRoute, command, cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.Created);
        sut.Headers.Location.Should().NotBeNull();

        var responsePublicId = await sut.Content.ReadFromJsonAsync<string>(cancellationToken);
        responsePublicId.Should().NotBeNullOrWhiteSpace();
        responsePublicId!.Should().StartWith("prd_");

        sut.Headers.Location!.ToString().Should().Contain($"{ProductsRoute}/{responsePublicId}");
        sut.GetIdFromLocationHeader().Should().Be(responsePublicId);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Name_Is_Missing()
    {
        var command = NewProductCommand() with { Name = "" };

        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .PostAsJsonAsync(ProductsRoute, command, CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Price_Is_Negative()
    {
        var command = NewProductCommand() with { Price = -1m };

        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .PostAsJsonAsync(ProductsRoute, command, CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
