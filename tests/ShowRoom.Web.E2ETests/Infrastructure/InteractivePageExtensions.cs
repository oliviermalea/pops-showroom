using Microsoft.Playwright;

namespace ShowRoom.Web.E2ETests.Infrastructure;

/// <summary>
/// Aides pour piloter un écran Blazor <c>InteractiveServer</c>.
/// </summary>
public static class InteractivePageExtensions
{
    /// <summary>
    /// Navigue et attend que le <b>circuit soit établi</b>.
    /// </summary>
    /// <remarks>
    /// Piège classique du rendu interactif : la page est d'abord préretournée en HTML statique, et ne
    /// devient interactive qu'une fois le WebSocket ouvert. Saisir avant cet instant part dans le vide —
    /// l'évènement est perdu et l'écran semble ignorer l'utilisateur. L'attente du WebSocket est un
    /// signal réel de disponibilité, contrairement à une temporisation fixe qui serait tantôt trop
    /// courte, tantôt du temps perdu.
    /// </remarks>
    public static async Task GotoInteractiveAsync(this IPage page, string url)
    {
        var circuit = page.WaitForWebSocketAsync();
        await page.GotoAsync(url);
        await circuit;
    }
}
