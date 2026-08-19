using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.Web.Features.Customer;
using ShowRoom.Web.Tests.Doubles;
using Xunit;
using DetailPage = ShowRoom.Web.Features.Customer.CustomerDetail.Page;

namespace ShowRoom.Web.Tests.Features.Customer.CustomerDetail;

/// <summary>
/// Scénarios de l'écran « détail client ». Un test = un état que l'utilisateur peut rencontrer.
/// </summary>
public sealed class PageTests : BunitContext
{
    private StubCustomerFacade Facade(TaskCompletionSource? gate = null)
    {
        var facade = new StubCustomerFacade(gate);
        Services.AddScoped<ICustomerFacade>(_ => facade);
        return facade;
    }

    private IRenderedComponent<DetailPage> RenderPage(string? publicId = CustomerSamples.PublicId)
        => Render<DetailPage>(p => p.Add(x => x.PublicId, publicId));

    [Fact]
    public void A_known_customer_is_displayed_with_its_fiche()
    {
        // Arrange
        Facade().LookupResult = CustomerLookupResult.Found(CustomerSamples.Detail());

        // Act
        var sut = RenderPage();

        // Assert
        sut.Find("article").TextContent.Should().Contain("Ada Lovelace").And.Contain("ada@showroom.test");
        sut.FindAll("[role='alert']").Should().BeEmpty();
    }

    [Fact]
    public void The_screen_asks_the_facade_for_the_public_id_of_the_route()
    {
        // Arrange
        var facade = Facade();
        facade.LookupResult = CustomerLookupResult.Found(CustomerSamples.Detail());

        // Act
        RenderPage();

        // Assert
        facade.LastPublicId.Should().Be(CustomerSamples.PublicId);
        facade.CallCount.Should().Be(1);
    }

    [Fact]
    public void An_unknown_customer_is_announced_as_an_error_naming_the_identifier()
    {
        // Arrange
        Facade().LookupResult = CustomerLookupResult.NotFound();

        // Act
        var sut = RenderPage();

        // Assert
        var alert = sut.Find("[role='alert']");
        alert.TextContent.Should().Contain("Aucun client").And.Contain(CustomerSamples.PublicId);
        sut.FindAll("article").Should().BeEmpty();
    }

    [Fact]
    public void A_malformed_identifier_explains_the_expected_format()
    {
        // Arrange
        Facade().LookupResult = CustomerLookupResult.InvalidPublicId();

        // Act
        var sut = RenderPage("pas-un-public-id");

        // Assert
        sut.Find("[role='alert']").TextContent.Should().Contain("Identifiant invalide").And.Contain("cus_");
    }

    [Fact]
    public void An_unreachable_service_offers_to_retry_on_the_same_screen()
    {
        // Arrange
        Facade().LookupResult = CustomerLookupResult.Unavailable();

        // Act
        var sut = RenderPage();

        // Assert
        var alert = sut.Find("[role='alert']");
        alert.TextContent.Should().Contain("momentanément indisponible");
        alert.QuerySelector("a")!.GetAttribute("href")
            .Should().Be($"/customers/{CustomerSamples.PublicId}", "réessayer ne doit pas quitter l'écran");
    }

    [Fact]
    public void The_orders_link_appears_only_once_the_customer_is_known()
    {
        // Arrange
        Facade().LookupResult = CustomerLookupResult.Found(CustomerSamples.Detail());

        // Act
        var sut = RenderPage();

        // Assert
        sut.Find($"a[href='/customers/{CustomerSamples.PublicId}/orders']").Should().NotBeNull();
    }

    [Fact]
    public void No_orders_link_is_offered_for_a_customer_that_could_not_be_loaded()
    {
        // Arrange
        Facade().LookupResult = CustomerLookupResult.NotFound();

        // Act
        var sut = RenderPage();

        // Assert
        sut.FindAll($"a[href='/customers/{CustomerSamples.PublicId}/orders']").Should().BeEmpty();
    }

    [Fact]
    public async Task While_the_service_answers_a_skeleton_holds_the_place_of_the_fiche()
    {
        // Arrange — la façade est suspendue : on observe l'écran pendant le chargement.
        var gate = new TaskCompletionSource();
        var facade = Facade(gate);
        facade.LookupResult = CustomerLookupResult.Found(CustomerSamples.Detail());

        // Act
        var sut = RenderPage();

        // Assert — pendant l'attente
        var loading = sut.Find("[role='status']");
        loading.GetAttribute("aria-busy").Should().Be("true");
        sut.FindAll("dd").Should().BeEmpty("aucune donnée n'est encore connue");

        // Act — la réponse arrive
        gate.SetResult();
        await sut.WaitForAssertionAsync(() => sut.FindAll("[role='status']").Should().BeEmpty());

        // Assert — après
        sut.Find("article").TextContent.Should().Contain("Ada Lovelace");
    }
}
