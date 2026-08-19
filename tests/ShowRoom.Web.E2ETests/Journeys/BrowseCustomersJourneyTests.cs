using System.Text.RegularExpressions;
using Microsoft.Playwright;
using ShowRoom.Web.E2ETests.Infrastructure;

namespace ShowRoom.Web.E2ETests.Journeys;

/// <summary>
/// Navigation réelle entre écrans SSR : les clics traversent le routeur et la navigation enrichie de
/// <c>blazor.web.js</c>, ce qu'aucun test HTTP ne reproduit — il ne fait que des GET indépendants.
/// </summary>
public sealed class BrowseCustomersJourneyTests(CustomerJourneyFixture fixture)
    : IClassFixture<CustomerJourneyFixture>
{
    [Fact]
    public async Task From_the_list_a_click_leads_to_the_fiche_then_to_the_orders()
    {
        // Arrange
        await fixture.SeedCustomerAsync("Margaret", "Hamilton", TestContext.Current.CancellationToken);
        var page = await fixture.NewPageAsync();

        // Act & Assert — la liste
        await page.GotoAsync("/customers");
        await Assertions.Expect(page.Locator("table")).ToContainTextAsync("Margaret Hamilton");

        // Act & Assert — la fiche, atteinte par un vrai clic
        await page.ClickAsync("a:has-text('Margaret Hamilton')");
        await Assertions.Expect(page.Locator("article")).ToContainTextAsync("Margaret Hamilton");

        // Act & Assert — les commandes ; le service Order n'est pas hébergé ici, l'agrégation dégrade
        // et l'écran doit le dire sans perdre l'identité du client.
        await page.ClickAsync("a:has-text('Voir les commandes')");
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Historique des commandes");
        await Assertions.Expect(page.Locator("[role='status']")).ToContainTextAsync("indisponible");
    }

    [Fact]
    public async Task The_search_filter_survives_a_reload_because_it_lives_in_the_url()
    {
        // Arrange
        await fixture.SeedCustomerAsync("Katherine", "Johnson", TestContext.Current.CancellationToken);
        await fixture.SeedCustomerAsync("Dorothy", "Vaughan", TestContext.Current.CancellationToken);
        var page = await fixture.NewPageAsync();
        await page.GotoAsync("/customers");

        // Act — le filtre est un formulaire GET, sans circuit
        await page.FillAsync("#customer-search", "Johnson");
        await page.ClickAsync(".filter__submit");

        // Assert
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"\?search=Johnson$"));
        await Assertions.Expect(page.Locator("table")).ToContainTextAsync("Katherine Johnson");
        await Assertions.Expect(page.Locator("table")).Not.ToContainTextAsync("Dorothy Vaughan");

        // Act — rechargement complet du navigateur
        await page.ReloadAsync();

        // Assert — l'état a survécu parce qu'il est dans l'URL, pas en mémoire du composant.
        await Assertions.Expect(page.Locator("table")).ToContainTextAsync("Katherine Johnson");
    }
}
