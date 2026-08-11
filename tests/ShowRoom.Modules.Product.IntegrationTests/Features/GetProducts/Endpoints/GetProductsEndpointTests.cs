namespace ShowRoom.Modules.Product.IntegrationTests.Features.GetProducts.Endpoints;

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ShowRoom.BuildingBlocks.Application.Pagination;
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
            $"{ProductsRoute}?page=1&pageSize=2",
            cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await sut.Content.ReadFromJsonAsync<PagedResult<ProductSummaryResponse>>(cancellationToken);
        body.Should().NotBeNull();
        body!.PageSize.Should().Be(2);
        body.Items.Should().HaveCount(2);
        body.TotalItems.Should().BeGreaterThanOrEqualTo(3);
        body.Page.Should().Be(1);
    }

    [Fact]
    public async Task Should_Filter_By_Product_PublicId()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var client = ConfiguredFactory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync(
            ProductsRoute, ProductNamed($"Kayak-{unique}"), cancellationToken);
        await client.PostAsJsonAsync(ProductsRoute, ProductNamed($"Voile-{unique}"), cancellationToken);

        var publicId = await createResponse.Content.ReadFromJsonAsync<string>(cancellationToken);
        publicId.Should().NotBeNullOrWhiteSpace();

        // Act
        HttpResponseMessage sut = await client.GetAsync(
            $"{ProductsRoute}?publicId={publicId}",
            cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await sut.Content.ReadFromJsonAsync<PagedResult<ProductSummaryResponse>>(cancellationToken);
        body.Should().NotBeNull();
        body!.Items.Should().ContainSingle();
        body.Items.Single().PublicId.Value.Should().Be(publicId);
        body.Items.Single().Name.Should().Be($"Kayak-{unique}");
        body.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task Should_Return_BadRequest_For_Malformed_PublicId_Filter()
    {
        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .GetAsync($"{ProductsRoute}?publicId=not-a-public-id", CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
