namespace ShowRoom.Modules.Product.IntegrationTests.Features.GetProducts.Endpoints;

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ShowRoom.Modules.Product;
using ShowRoom.Modules.Product.Features.CreateProduct;
using ShowRoom.Modules.Product.Features.GetProducts;
using ShowRoom.Testing.Configuration;
using ShowRoom.Testing.Database;

public class GetProductsEndpointTests(
    ProductBusinessWebFactory applicationInMemoryFactory,
    DatabaseContainer database)
    : IClassFixture<ProductBusinessWebFactory>,
      IClassFixture<DatabaseContainer>
{
    private static readonly string ProductsRoute = $"/api/v1/{ProductModule.RouteSegment}";

    private WebApplicationFactory<Program> ConfiguredFactory =>
        applicationInMemoryFactory
            .WithContainerDatabaseConfigured(new ProductDatabaseConfiguration(database.ConnectionString!));

    private static CreateProductCommand ProductNamed(string name) => new()
    {
        Name = name,
        Description = null,
        Price = 19.90m,
        Currency = "EUR",
    };

    [Fact]
    public async Task Should_Paginate_Results()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var client = ConfiguredFactory.CreateClient();
        var tag = Guid.NewGuid().ToString("N");

        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync(ProductsRoute, ProductNamed($"Planche {tag} {i}"), cancellationToken);
        }

        // Act
        HttpResponseMessage sut = await client.GetAsync(
            $"{ProductsRoute}?search=Planche {tag}&page=1&pageSize=2",
            cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await sut.Content.ReadFromJsonAsync<GetProductsResponse>(cancellationToken);
        body.Should().NotBeNull();
        body!.PageSize.Should().Be(2);
        body.Products.Should().HaveCount(2);
        body.TotalItems.Should().Be(3);
        body.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Should_Filter_By_Name_Search_Case_Insensitively()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var client = ConfiguredFactory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];

        await client.PostAsJsonAsync(ProductsRoute, ProductNamed($"Kayak-{unique}"), cancellationToken);
        await client.PostAsJsonAsync(ProductsRoute, ProductNamed($"Voile-{unique}"), cancellationToken);

        // Act (lower-case search must still match the capitalised name via ILike)
        HttpResponseMessage sut = await client.GetAsync(
            $"{ProductsRoute}?search=kayak-{unique}",
            cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await sut.Content.ReadFromJsonAsync<GetProductsResponse>(cancellationToken);
        body.Should().NotBeNull();
        body!.Products.Should().ContainSingle();
        body.Products.Single().Name.Should().Be($"Kayak-{unique}");
    }
}
