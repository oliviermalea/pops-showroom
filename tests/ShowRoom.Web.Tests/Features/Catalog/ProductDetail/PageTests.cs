using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.Web.Client.Features.Catalog;
using ShowRoom.Web.Client.Features.Catalog.ProductDetail;
using ShowRoom.Web.Shared.Api.Problems;
using ShowRoom.Web.Tests.Doubles;
using Xunit;
using DetailPage = ShowRoom.Web.Client.Features.Catalog.ProductDetail.Page;

namespace ShowRoom.Web.Tests.Features.Catalog.ProductDetail;

public sealed class PageTests : BunitContext
{
    private const string ValidPublicId = "prd_0123456789abcdef0123456789abcdef";

    private StubCatalogFacade Facade()
    {
        var facade = new StubCatalogFacade();
        Services.AddScoped<ICatalogFacade>(_ => facade);
        return facade;
    }

    private IRenderedComponent<DetailPage> RenderPage(string? publicId = ValidPublicId)
        => Render<DetailPage>(parameters => parameters.Add(page => page.PublicId, publicId));

    [Fact]
    public void A_found_product_shows_its_price_status_and_public_id()
    {
        // Arrange
        var facade = Facade();
        facade.LookupResult = ProductLookupResult.Found(new ProductDetailView(
            ValidPublicId, "Bureau Compas", "Plateau chêne", "1 180,00 EUR", "EUR", "Available", true));

        // Act
        var sut = RenderPage();

        // Assert
        var card = sut.Find("article");
        card.TextContent.Should().Contain("Bureau Compas");
        card.TextContent.Should().Contain("1 180,00 EUR");
        card.TextContent.Should().Contain(ValidPublicId);
    }

    [Fact]
    public void An_invalid_public_id_is_explained_rather_than_reported_as_a_failure()
    {
        // Arrange
        var facade = Facade();
        facade.LookupResult = ProductLookupResult.InvalidPublicId();

        // Act
        var sut = RenderPage("pas-un-id");

        // Assert
        sut.Find("[role='alert']").TextContent.Should().Contain("Identifiant invalide");
    }

    [Fact]
    public void A_missing_product_carries_the_api_problem()
    {
        // Arrange
        var facade = Facade();
        facade.LookupResult = ProductLookupResult.NotFound(new ApiProblem(
            404, "Not Found", "No product…", "00-trace-02",
            [new ApiProblemError("Product.NotFound", "…")]));

        // Act
        var sut = RenderPage();

        // Assert
        sut.Find("[role='alert']").TextContent.Should().Contain("Aucun produit");
        sut.Find("details.problem").TextContent.Should().Contain("Product.NotFound");
    }

    [Fact]
    public void An_unavailable_catalogue_offers_a_retry()
    {
        // Arrange
        var facade = Facade();
        facade.LookupResult = ProductLookupResult.Unavailable();

        // Act
        var sut = RenderPage();

        // Assert
        sut.Find("button.product__retry").Should().NotBeNull();
        facade.LastPublicId.Should().Be(ValidPublicId);
    }
}
