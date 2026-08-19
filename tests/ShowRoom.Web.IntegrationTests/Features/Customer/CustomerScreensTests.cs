using System.Net;
using AwesomeAssertions;
using ShowRoom.Web.IntegrationTests.Infrastructure;

namespace ShowRoom.Web.IntegrationTests.Features.Customer;

/// <summary>
/// Chaîne complète, sur une base montée puis détruite : le front rend son HTML SSR à partir de données
/// réellement écrites par l'API dans PostgreSQL.
///
/// <para><b>Critère d'admission, volontairement strict</b> : un test n'a sa place ici que si le
/// conteneur apporte un signal que les tests de composants ne peuvent pas produire — route réelle,
/// query string traduite en SQL, sérialisation aller-retour, en-têtes du pipeline. Tout ce qui se
/// prouve sans réseau (messages d'état, rejets côté client, rendu) reste dans
/// <c>ShowRoom.Web.Tests</c>, qui tourne en une seconde. Cette suite doit rester courte : quelques
/// scénarios critiques valent mieux qu'une suite lente.</para>
/// </summary>
public sealed class CustomerScreensTests(CustomerFrontFixture fixture) : IClassFixture<CustomerFrontFixture>
{
    [Fact]
    public async Task The_list_shows_the_customers_that_really_exist_in_the_database()
    {
        // Arrange
        var ada = await fixture.SeedCustomerAsync("Ada", "Lovelace", cancellationToken: TestContext.Current.CancellationToken);
        await fixture.SeedCustomerAsync("Grace", "Hopper", cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var html = await fixture.GetPageAsync("/customers", TestContext.Current.CancellationToken);

        // Assert
        html.Should().Contain("Ada Lovelace").And.Contain("Grace Hopper");
        html.Should().Contain($"/customers/{ada}", "chaque ligne renvoie vers la fiche du client");
    }

    [Fact]
    public async Task The_search_filter_travels_through_the_query_string_down_to_the_sql_query()
    {
        // Arrange
        await fixture.SeedCustomerAsync("Katherine", "Johnson", cancellationToken: TestContext.Current.CancellationToken);
        await fixture.SeedCustomerAsync("Dorothy", "Vaughan", cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var html = await fixture.GetPageAsync("/customers?search=Johnson", TestContext.Current.CancellationToken);

        // Assert
        html.Should().Contain("Katherine Johnson");
        html.Should().NotContain("Dorothy Vaughan");
    }

    [Fact]
    public async Task The_detail_screen_renders_the_customer_written_by_the_api()
    {
        // Arrange
        var publicId = await fixture.SeedCustomerAsync("Barbara", "Liskov", "barbara.liskov@showroom.test", cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var html = await fixture.GetPageAsync($"/customers/{publicId}", TestContext.Current.CancellationToken);

        // Assert
        html.Should().Contain("Barbara Liskov")
            .And.Contain("barbara.liskov@showroom.test")
            .And.Contain(publicId);
    }

    [Fact]
    public async Task An_unknown_customer_is_reported_by_the_screen_not_by_a_500()
    {
        // Arrange — un identifiant bien formé mais absent de la base.
        var missing = "cus_" + new string('a', 32);

        // Act
        var response = await fixture.Front.GetAsync($"/customers/{missing}", TestContext.Current.CancellationToken);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "l'écran gère le cas, il ne plante pas");
        html.Should().Contain("Aucun client ne correspond");
    }

    [Fact]
    public async Task The_orders_screen_reports_an_empty_history_when_the_order_service_answers_nothing()
    {
        // Arrange
        var publicId = await fixture.SeedCustomerAsync("Margaret", "Hamilton", cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var html = await fixture.GetPageAsync($"/customers/{publicId}/orders", TestContext.Current.CancellationToken);

        // Assert — le service Order n'est pas hébergé ici : l'agrégation dégrade, et l'écran le dit
        // sans jamais perdre l'identité du client.
        html.Should().Contain("Margaret Hamilton");
        html.Should().Contain("indisponible").And.NotContain("Aucun client ne correspond");
    }

    /// <summary>
    /// Les écrans portant des données client ne doivent jamais être stockés par un navigateur ou un
    /// proxy : la politique est vérifiée sur la réponse réelle, pas sur l'intention.
    /// </summary>
    [Fact]
    public async Task Customer_screens_forbid_any_caching()
    {
        // Arrange
        var publicId = await fixture.SeedCustomerAsync("Radia", "Perlman", cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await fixture.Front.GetAsync($"/customers/{publicId}", TestContext.Current.CancellationToken);

        // Assert
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        response.Headers.CacheControl.NoCache.Should().BeTrue();
    }
}
