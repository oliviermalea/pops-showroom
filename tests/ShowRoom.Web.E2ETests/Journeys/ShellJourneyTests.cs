using System.Text.RegularExpressions;
using Microsoft.Playwright;
using ShowRoom.Web.E2ETests.Infrastructure;

namespace ShowRoom.Web.E2ETests.Journeys;

/// <summary>
/// La coquille est pilotée par du JavaScript pur (<c>wwwroot/App/Layout/shell.js</c>) précisément pour
/// ne pas ouvrir de circuit sur les écrans SSR. Conséquence : <b>seul un navigateur peut la tester</b> —
/// c'est le point que j'avais dû simuler à la main faute d'outil.
/// </summary>
public sealed class ShellJourneyTests(CustomerJourneyFixture fixture) : IClassFixture<CustomerJourneyFixture>
{
    private static readonly Regex Visible = new("is-visible");

    [Fact]
    public async Task The_mobile_menu_opens_and_closes_without_any_blazor_circuit()
    {
        // Arrange — viewport mobile : le menu burger n'existe qu'en dessous de 768 px.
        var page = await fixture.NewPageAsync(width: 375, height: 812);
        await page.GotoAsync("/customers");

        var toggle = page.Locator(".menu-toggle");
        var navigation = page.Locator(".site-nav");
        await Assertions.Expect(navigation).Not.ToBeVisibleAsync();

        // Act — ouverture
        await toggle.ClickAsync();

        // Assert
        await Assertions.Expect(navigation).ToBeVisibleAsync();
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");

        // Act — fermeture par le fond
        await page.Locator(".menu-backdrop").ClickAsync();

        // Assert
        await Assertions.Expect(navigation).Not.ToBeVisibleAsync();
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public async Task The_back_to_top_button_appears_on_scroll_and_returns_to_the_top()
    {
        // Arrange — assez de clients pour que la page défile réellement.
        for (var i = 0; i < 25; i++)
        {
            await fixture.SeedCustomerAsync($"Client{i}", $"Defilement{i}", TestContext.Current.CancellationToken);
        }

        var page = await fixture.NewPageAsync(width: 375, height: 640);
        await page.GotoAsync("/customers");

        var button = page.Locator(".scroll-top");
        await Assertions.Expect(button).Not.ToHaveClassAsync(Visible);

        // Act — défilement réel, puis retour en haut par le bouton
        await page.Mouse.WheelAsync(0, 1500);
        await Assertions.Expect(button).ToHaveClassAsync(Visible);
        await button.ClickAsync();

        // Assert — le bouton se masque de lui-même une fois revenu en haut.
        await Assertions.Expect(button).Not.ToHaveClassAsync(Visible);
    }
}
