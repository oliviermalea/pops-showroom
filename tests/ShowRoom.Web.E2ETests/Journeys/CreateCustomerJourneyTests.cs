using System.Text.RegularExpressions;
using AwesomeAssertions;
using Microsoft.Playwright;
using ShowRoom.Web.E2ETests.Infrastructure;

namespace ShowRoom.Web.E2ETests.Journeys;

/// <summary>
/// Parcours de création, dans un vrai navigateur. C'est le seul écran en <c>InteractiveServer</c> : sa
/// validation au fil de la saisie et sa soumission passent par un circuit SignalR que ni bUnit (qui
/// simule le rendu) ni les tests HTTP (qui ne rendent que du SSR) n'exercent.
/// </summary>
public sealed class CreateCustomerJourneyTests(CustomerJourneyFixture fixture)
    : IClassFixture<CustomerJourneyFixture>
{
    private static readonly Regex FichePattern = new(@"/customers/cus_[0-9a-f]{32}$");

    [Fact]
    public async Task A_new_customer_is_created_and_the_browser_lands_on_its_fiche()
    {
        // Arrange
        var page = await fixture.NewPageAsync();
        var email = $"ada.{Guid.NewGuid():N}@showroom.test";
        await page.GotoInteractiveAsync("/customers/new");

        // Act
        await page.FillAsync("#firstName", "Ada");
        await page.FillAsync("#lastName", "Lovelace");
        await page.FillAsync("#email", email);
        await page.FillAsync("#phone", "+33123456789");
        await page.ClickAsync("button[type='submit']");

        // Assert — la redirection mène à la fiche du client réellement écrit en base. On assere l'URL
        // (avec réessai) plutôt que d'attendre un évènement `load` : une navigation Blazor passe par
        // l'historique et n'en produit pas.
        await Assertions.Expect(page).ToHaveURLAsync(FichePattern);

        var fiche = page.Locator("article");
        await Assertions.Expect(fiche).ToContainTextAsync("Ada Lovelace");
        await Assertions.Expect(fiche).ToContainTextAsync(email);
        await Assertions.Expect(fiche).ToContainTextAsync("+33123456789");
    }

    [Fact]
    public async Task The_form_refuses_a_malformed_email_while_the_user_types()
    {
        // Arrange
        var page = await fixture.NewPageAsync();
        await page.GotoInteractiveAsync("/customers/new");

        // Act — la validation se déclenche à la sortie du champ, via le circuit.
        await page.FillAsync("#email", "pas-un-email");
        await page.ClickAsync("#firstName");

        // Assert
        await Assertions.Expect(page.Locator(".validation-message"))
            .ToContainTextAsync("Saisissez une adresse email valide.");
        page.Url.Should().EndWith("/customers/new", "aucune soumission n'a eu lieu");
    }

    [Fact]
    public async Task A_duplicate_email_is_reported_without_leaving_the_form()
    {
        // Arrange — un client existe déjà avec cet email, créé par le formulaire lui-même.
        var page = await fixture.NewPageAsync();
        var email = $"grace.{Guid.NewGuid():N}@showroom.test";
        await page.GotoInteractiveAsync("/customers/new");
        await SubmitFormAsync(page, "Grace", "Hopper", email);
        await Assertions.Expect(page).ToHaveURLAsync(FichePattern);

        // Act — on rejoue exactement le même email.
        await page.GotoInteractiveAsync("/customers/new");
        await SubmitFormAsync(page, "Grace", "Hopper", email);

        // Assert
        await Assertions.Expect(page.Locator("[role='alert']")).ToContainTextAsync("utilise déjà");
        page.Url.Should().EndWith("/customers/new");
    }

    private static async Task SubmitFormAsync(IPage page, string firstName, string lastName, string email)
    {
        await page.FillAsync("#firstName", firstName);
        await page.FillAsync("#lastName", lastName);
        await page.FillAsync("#email", email);
        await page.ClickAsync("button[type='submit']");
    }
}
