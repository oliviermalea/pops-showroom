using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.Web.Features.Customer;
using ShowRoom.Web.Tests.Doubles;
using Xunit;
using OrdersPage = ShowRoom.Web.Features.Customer.CustomerOrders.Page;

namespace ShowRoom.Web.Tests.Features.Customer.CustomerOrders;

/// <summary>
/// Scénarios de l'écran « historique des commandes ». Il agrège un autre service par le bus : la
/// distinction entre « pas de commande » et « service injoignable » est le comportement clé.
/// </summary>
public sealed class PageTests : BunitContext
{
    private StubCustomerFacade Facade()
    {
        var facade = new StubCustomerFacade();
        Services.AddScoped<ICustomerFacade>(_ => facade);
        return facade;
    }

    private IRenderedComponent<OrdersPage> RenderPage(string? publicId = CustomerSamples.PublicId)
        => Render<OrdersPage>(p => p.Add(x => x.PublicId, publicId));

    [Fact]
    public void The_history_lists_every_order_of_the_customer()
    {
        // Arrange
        var orders = new[] { CustomerSamples.Order("ord_1"), CustomerSamples.Order("ord_2") };
        Facade().OrdersResult = CustomerOrdersResult.Found(CustomerSamples.Orders(orders: orders));

        // Act
        var sut = RenderPage();

        // Assert
        sut.FindAll("article").Should().HaveCount(2);
        sut.Markup.Should().Contain("Ada Lovelace").And.Contain("608,80 EUR");
    }

    /// <summary>
    /// Le cœur du sujet : le backend a répondu 200 avec ordersAvailable = false. L'identité du client
    /// reste exacte, seules les commandes manquent — ce n'est ni une erreur, ni « aucune commande ».
    /// </summary>
    [Fact]
    public void An_unreachable_order_service_is_announced_as_a_partial_result_not_as_an_error()
    {
        // Arrange
        Facade().OrdersResult = CustomerOrdersResult.Found(CustomerSamples.Orders(ordersAvailable: false));

        // Act
        var sut = RenderPage();

        // Assert
        var banner = sut.Find("[role='status']");
        banner.TextContent.Should().Contain("indisponible").And.Contain("Order");

        sut.FindAll("[role='alert']").Should().BeEmpty("la requête a réussi : ce n'est pas une erreur");
        sut.Markup.Should().Contain("Ada Lovelace", "l'identité du client reste connue");
        sut.Markup.Should().NotContain("n'a pas encore passé de commande", "on ignore s'il en a");
    }

    [Fact]
    public void The_degraded_banner_offers_to_retry_without_leaving_the_screen()
    {
        // Arrange
        Facade().OrdersResult = CustomerOrdersResult.Found(CustomerSamples.Orders(ordersAvailable: false));

        // Act
        var sut = RenderPage();

        // Assert
        sut.Find("[role='status'] a").GetAttribute("href")
            .Should().Be($"/customers/{CustomerSamples.PublicId}/orders");
    }

    [Fact]
    public void A_customer_without_any_order_gets_a_different_message_than_a_degraded_one()
    {
        // Arrange
        Facade().OrdersResult = CustomerOrdersResult.Found(
            CustomerSamples.Orders(ordersAvailable: true, orders: []));

        // Act
        var sut = RenderPage();

        // Assert
        sut.Markup.Should().Contain("n'a pas encore passé de commande");
        sut.FindAll("[role='status']").Should().BeEmpty("le service a répondu, rien n'est dégradé");
        sut.FindAll("article").Should().BeEmpty();
    }

    [Fact]
    public void An_unknown_customer_is_announced_as_an_error()
    {
        // Arrange
        Facade().OrdersResult = CustomerOrdersResult.NotFound();

        // Act
        var sut = RenderPage();

        // Assert
        sut.Find("[role='alert']").TextContent.Should().Contain("Aucun client");
    }

    [Fact]
    public void An_unreachable_customer_service_is_an_error_with_a_retry()
    {
        // Arrange
        Facade().OrdersResult = CustomerOrdersResult.Unavailable();

        // Act
        var sut = RenderPage();

        // Assert
        var alert = sut.Find("[role='alert']");
        alert.TextContent.Should().Contain("momentanément indisponible");
        alert.QuerySelector("a").Should().NotBeNull();
    }

    [Fact]
    public void The_screen_always_offers_a_way_back_to_the_fiche()
    {
        // Arrange
        Facade().OrdersResult = CustomerOrdersResult.Found(CustomerSamples.Orders());

        // Act
        var sut = RenderPage();

        // Assert
        sut.Find($"a[href='/customers/{CustomerSamples.PublicId}']").TextContent
            .Should().Contain("Retour");
    }
}
