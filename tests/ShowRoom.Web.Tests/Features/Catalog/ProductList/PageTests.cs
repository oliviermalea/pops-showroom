using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.Web.Client.Features.Catalog;
using ShowRoom.Web.Client.Features.Catalog.ProductList;
using ShowRoom.Web.Shared.Api.Problems;
using ShowRoom.Web.Tests.Doubles;
using Xunit;
using CatalogPage = ShowRoom.Web.Client.Features.Catalog.ProductList.Page;

namespace ShowRoom.Web.Tests.Features.Catalog.ProductList;

/// <summary>
/// Scénarios de l'écran catalogue. bUnit rend le composant directement : le render mode
/// (<c>InteractiveWebAssembly</c>) ne change rien ici, ce qui est justement l'intérêt — le même test
/// vaut quel que soit l'endroit où le composant s'exécutera.
/// </summary>
public sealed class PageTests : BunitContext
{
    private StubCatalogFacade Facade(TaskCompletionSource? gate = null)
    {
        var facade = new StubCatalogFacade(gate);
        Services.AddScoped<ICatalogFacade>(_ => facade);
        return facade;
    }

    private static ProductListView PageOf(params string[] names) => new(
        [.. names.Select((name, i) => new ProductListItemView($"prd_{i}", name, "345,00 EUR", "Available", true))],
        Page: 1,
        PageSize: 12,
        TotalItems: names.Length,
        TotalPages: 1);

    [Fact]
    public void It_renders_one_card_per_product_with_a_link_to_its_detail()
    {
        // Arrange
        var facade = Facade();
        facade.ListResult = ProductListResult.Loaded(PageOf("Chaise", "Lampe"));

        // Act
        var sut = Render<CatalogPage>();

        // Assert
        sut.FindAll("article.card").Should().HaveCount(2);
        sut.Find("a[href='/catalog/prd_0']").TextContent.Should().Be("Chaise");
    }

    [Fact]
    public void An_empty_catalogue_says_so_instead_of_showing_an_empty_grid()
    {
        // Arrange
        var facade = Facade();
        facade.ListResult = ProductListResult.Loaded(new ProductListView([], 1, 12, 0, 0));

        // Act
        var sut = Render<CatalogPage>();

        // Assert
        sut.FindAll("article.card").Should().BeEmpty();
        sut.Find(".catalog__state--muted").TextContent.Should().Contain("Aucun produit");
    }

    [Fact]
    public void An_unavailable_catalogue_offers_a_retry_and_shows_the_api_problem()
    {
        // Arrange
        var facade = Facade();
        facade.ListResult = ProductListResult.Unavailable(new ApiProblem(
            503, "Service Unavailable", null, "00-trace-01", []));

        // Act
        var sut = Render<CatalogPage>();

        // Assert
        sut.Find("[role='alert']").TextContent.Should().Contain("momentanément indisponible");
        sut.Find("details.problem").TextContent.Should().Contain("00-trace-01");
    }

    [Fact]
    public void A_transport_failure_shows_no_diagnostic_panel()
    {
        // Arrange — pas de corps de réponse, donc rien de technique à montrer.
        var facade = Facade();
        facade.ListResult = ProductListResult.Unavailable();

        // Act
        var sut = Render<CatalogPage>();

        // Assert
        sut.FindAll("details.problem").Should().BeEmpty();
    }

    [Fact]
    public async Task While_the_service_answers_a_skeleton_holds_the_place_of_the_grid()
    {
        // Arrange — la façade est suspendue : on observe l'écran pendant le chargement.
        var gate = new TaskCompletionSource();
        var facade = Facade(gate);
        facade.ListResult = ProductListResult.Loaded(PageOf("Chaise"));

        // Act
        var sut = Render<CatalogPage>();

        // Assert — pendant l'attente
        var loading = sut.Find("[role='status']");
        loading.GetAttribute("aria-busy").Should().Be("true");
        sut.FindAll("article.card--skeleton").Should().NotBeEmpty();

        // Act — la réponse arrive
        gate.SetResult();
        await sut.WaitForAssertionAsync(() => sut.FindAll("[role='status']").Should().BeEmpty());

        // Assert — après
        sut.Find("article.card").TextContent.Should().Contain("Chaise");
    }

    [Fact]
    public void Paging_asks_the_facade_for_the_next_page_without_leaving_the_screen()
    {
        // Arrange — deux pages, donc « Suivant » est actif.
        var facade = Facade();
        facade.ListResult = ProductListResult.Loaded(new ProductListView(
            [new ProductListItemView("prd_0", "Chaise", "345,00 EUR", "Available", true)],
            Page: 1, PageSize: 12, TotalItems: 18, TotalPages: 2));

        var sut = Render<CatalogPage>();

        // Act
        sut.FindAll("button.catalog__page").First(button => button.TextContent.Contains("Suivant")).Click();

        // Assert — c'est tout l'intérêt du WebAssembly : seul l'appel HTTP repart, pas la page.
        facade.LastListQuery!.Value.Page.Should().Be(2);
        facade.CallCount.Should().Be(2);
    }

    [Fact]
    public void Paging_is_disabled_when_there_is_nowhere_to_go()
    {
        // Arrange — une seule page.
        var facade = Facade();
        facade.ListResult = ProductListResult.Loaded(PageOf("Chaise"));

        // Act
        var sut = Render<CatalogPage>();

        // Assert
        sut.FindAll("button.catalog__page").Should().AllSatisfy(button =>
            button.HasAttribute("disabled").Should().BeTrue());
    }
}
