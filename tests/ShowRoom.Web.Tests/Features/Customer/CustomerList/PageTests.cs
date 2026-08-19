using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.Web.Features.Customer;
using ShowRoom.Web.Tests.Doubles;
using Xunit;
using ListPage = ShowRoom.Web.Features.Customer.CustomerList.Page;

namespace ShowRoom.Web.Tests.Features.Customer.CustomerList;

/// <summary>
/// Scénarios de l'écran « liste des clients ». L'écran est en SSR statique : son état vit dans la
/// query string, et ses interactions sont un formulaire GET et des liens — c'est ce contrat qui est
/// vérifié ici, pas des gestionnaires d'événements.
/// </summary>
public sealed class PageTests : BunitContext
{
    private StubCustomerFacade Facade(TaskCompletionSource? gate = null)
    {
        var facade = new StubCustomerFacade(gate);
        Services.AddScoped<ICustomerFacade>(_ => facade);
        return facade;
    }

    private void GoTo(string relativeUrl)
        => Services.GetRequiredService<NavigationManager>().NavigateTo(relativeUrl);

    [Fact]
    public void The_first_page_is_requested_when_the_url_carries_no_state()
    {
        // Arrange
        var facade = Facade();
        facade.ListResult = CustomerListResult.Loaded(CustomerSamples.ListPage());

        // Act
        Render<ListPage>();

        // Assert
        facade.LastListQuery.Should().Be((1, 20, (string?)null));
    }

    [Fact]
    public void Page_and_search_are_read_from_the_query_string()
    {
        // Arrange
        var facade = Facade();
        facade.ListResult = CustomerListResult.Loaded(CustomerSamples.ListPage(page: 2, totalPages: 3));
        GoTo("/customers?page=2&search=ada");

        // Act
        Render<ListPage>();

        // Assert
        facade.LastListQuery.Should().Be((2, 20, "ada"));
    }

    [Fact]
    public void The_loaded_page_shows_the_customers_and_the_position_in_the_set()
    {
        // Arrange
        Facade().ListResult = CustomerListResult.Loaded(
            CustomerSamples.ListPage(page: 1, totalItems: 42, totalPages: 3));

        // Act
        var sut = Render<ListPage>();

        // Assert
        sut.FindAll("tbody tr").Should().HaveCount(2);
        sut.Find("nav[aria-label='Pagination']").TextContent
            .Should().Contain("1–2 sur 42").And.Contain("page 1 / 3");
    }

    [Fact]
    public void Filtering_goes_through_a_get_form_so_the_state_lands_in_the_url()
    {
        // Arrange
        Facade().ListResult = CustomerListResult.Loaded(CustomerSamples.ListPage());

        // Act
        var sut = Render<ListPage>();

        // Assert
        var form = sut.Find("form");
        form.GetAttribute("method").Should().Be("get");
        form.GetAttribute("action").Should().Be("/customers");
        form.QuerySelector("input[name='search']").Should().NotBeNull();
    }

    [Fact]
    public void The_first_page_offers_a_next_link_and_an_inert_previous_control()
    {
        // Arrange
        Facade().ListResult = CustomerListResult.Loaded(
            CustomerSamples.ListPage(page: 1, totalItems: 42, totalPages: 3));

        // Act
        var sut = Render<ListPage>();

        // Assert
        sut.Find("a[rel='next']").GetAttribute("href").Should().Be("/customers?page=2");
        sut.FindAll("a[rel='prev']").Should().BeEmpty();
        sut.Find("[aria-disabled='true']").TextContent.Should().Contain("Précédent");
    }

    [Fact]
    public void Pagination_links_keep_the_active_search()
    {
        // Arrange
        Facade().ListResult = CustomerListResult.Loaded(
            CustomerSamples.ListPage(page: 2, totalItems: 60, totalPages: 3));
        GoTo("/customers?page=2&search=ada");

        // Act
        var sut = Render<ListPage>();

        // Assert
        sut.Find("a[rel='next']").GetAttribute("href").Should().Be("/customers?page=3&search=ada");
        sut.Find("a[rel='prev']").GetAttribute("href").Should().Be("/customers?search=ada");
    }

    [Fact]
    public void An_empty_set_and_an_empty_search_result_do_not_say_the_same_thing()
    {
        // Arrange
        Facade().ListResult = CustomerListResult.Loaded(
            CustomerSamples.ListPage(totalItems: 0, totalPages: 0, items: []));

        // Act
        var sut = Render<ListPage>();

        // Assert
        sut.Markup.Should().Contain("Aucun client enregistré");
    }

    [Fact]
    public void A_search_without_result_names_the_term_that_was_looked_for()
    {
        // Arrange
        Facade().ListResult = CustomerListResult.Loaded(
            CustomerSamples.ListPage(totalItems: 0, totalPages: 0, items: []));
        GoTo("/customers?search=zzz");

        // Act
        var sut = Render<ListPage>();

        // Assert
        sut.Markup.Should().Contain("Aucun client ne correspond").And.Contain("zzz");
        sut.Find("a[href='/customers']").Should().NotBeNull("un moyen d'effacer le filtre doit rester offert");
    }

    [Fact]
    public void An_unreachable_service_offers_to_retry_on_the_same_page()
    {
        // Arrange
        Facade().ListResult = CustomerListResult.Unavailable();
        GoTo("/customers?page=3");

        // Act
        var sut = Render<ListPage>();

        // Assert
        var alert = sut.Find("[role='alert']");
        alert.TextContent.Should().Contain("momentanément indisponible");
        alert.QuerySelector("a")!.GetAttribute("href").Should().Be("/customers?page=3");
    }

    [Fact]
    public async Task While_the_service_answers_a_skeleton_holds_the_place_and_the_pager_keeps_its_room()
    {
        // Arrange
        var gate = new TaskCompletionSource();
        var facade = Facade(gate);
        facade.ListResult = CustomerListResult.Loaded(CustomerSamples.ListPage());

        // Act
        var sut = Render<ListPage>();

        // Assert — pendant l'attente
        sut.Find("[role='status'][aria-busy='true']").Should().NotBeNull();
        sut.FindAll("tbody tr").Should().HaveCount(5, "le squelette occupe la place du tableau");
        sut.Find("nav[aria-label='Pagination']").ClassList.Should().Contain("pager--hidden");

        // Act — la réponse arrive
        gate.SetResult();
        await sut.WaitForAssertionAsync(() => sut.FindAll("[aria-busy='true']").Should().BeEmpty());

        // Assert — après
        sut.FindAll("tbody tr").Should().HaveCount(2);
        sut.Find("nav[aria-label='Pagination']").ClassList.Should().NotContain("pager--hidden");
    }
}
