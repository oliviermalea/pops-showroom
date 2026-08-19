using Microsoft.Playwright;

namespace ShowRoom.Web.E2ETests.Infrastructure;

/// <summary>
/// Aides pour piloter un écran Blazor <c>InteractiveServer</c>.
/// </summary>
public static class InteractivePageExtensions
{
    /// <summary>Nombre de sondes avant de déclarer le circuit inexploitable.</summary>
    private const int ProbeAttempts = 10;

    /// <summary>Délai laissé au circuit pour réagir à une sonde.</summary>
    private const int ProbeTimeoutMs = 1_000;

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

    /// <summary>
    /// Navigue vers l'écran de création et n'en sort qu'une fois son circuit <b>réellement réactif</b>.
    /// </summary>
    /// <remarks>
    /// <para>Le WebSocket ouvert ne suffit pas : entre sa connexion et l'attachement des gestionnaires
    /// d'évènements, la toute première saisie peut encore être <b>perdue</b> — et rien ne la rejoue.
    /// Absorber ce coût une seule fois au montage de la fixture ne protège que le premier circuit :
    /// chaque navigation en ouvre un neuf et rejoue la même course. La sonde appartient donc à la
    /// navigation, pas à la fixture.</para>
    ///
    /// <para>Elle est <b>réessayée</b> jusqu'à obtenir une réaction observable — seul moyen de
    /// distinguer « pas encore prêt » de « cassé », là où une temporisation fixe serait tantôt trop
    /// courte, tantôt du temps perdu. Le formulaire est rendu vierge avant de sortir.</para>
    /// </remarks>
    public static async Task GotoCreateCustomerAsync(this IPage page)
    {
        await page.GotoInteractiveAsync("/customers/new");

        for (var attempt = 1; attempt <= ProbeAttempts; attempt++)
        {
            // Une saisie invalide provoque un message de validation : réaction observable, et sans effet
            // de bord côté serveur (rien n'est envoyé).
            await page.FillAsync("#email", $"sonde-{attempt}");
            await page.ClickAsync("#firstName");

            try
            {
                await page.Locator(".validation-message").First.WaitForAsync(
                    new LocatorWaitForOptions { Timeout = ProbeTimeoutMs });

                await page.FillAsync("#email", string.Empty);
                return;
            }
            catch (TimeoutException)
            {
                // Les gestionnaires ne sont pas encore attachés : on rejoue la saisie.
            }
        }

        throw new InvalidOperationException(
            $"Le circuit interactif n'a pas réagi après {ProbeAttempts} sondes : l'écran de création " +
            "n'est pas exploitable, inutile de poursuivre le parcours.");
    }
}
