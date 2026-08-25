using ShowRoom.Web.Shared.Api.Problems;

namespace ShowRoom.Web.Features.Customer;

/// <summary>
/// Turns the API's error <b>codes</b> into the French sentences this UI shows.
/// </summary>
/// <remarks>
/// <para>The code is the stable half of the ProblemDetails contract; the <c>detail</c> text is the
/// server's own wording, in the API's language (English here). Rendering that text straight into the UI
/// would put a foreign, backend-flavoured sentence in front of the user and couple the screen to a
/// string the backend is free to reword. So the UI resolves its message from the code and keeps the
/// server text for the diagnostic panel.</para>
///
/// <para>An unknown code falls back to the caller's generic sentence — never to a blank message, and
/// never to the raw server text.</para>
/// </remarks>
public static class CustomerProblemMessages
{
    private static readonly Dictionary<string, string> ByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Customer.NotFound"] = "Aucun client ne correspond à cet identifiant.",
        ["Customer.EmailAlreadyExists"] = "Un client utilise déjà cette adresse email.",
        ["Customer.NameRequired"] = "Le prénom et le nom sont obligatoires.",
        ["Validation.FirstName"] = "Le prénom est obligatoire et limité à 200 caractères.",
        ["Validation.LastName"] = "Le nom est obligatoire et limité à 200 caractères.",
        ["Validation.Email"] = "Saisissez une adresse email valide (320 caractères au plus).",
        ["Validation.Phone"] = "Le téléphone est limité à 40 caractères.",
        ["Validation.General"] = "La saisie a été refusée par le service.",
        ["Email.Invalid"] = "Saisissez une adresse email valide.",
        ["PhoneNumber.Invalid"] = "Le numéro de téléphone est invalide.",
    };

    /// <summary>The French message for a known code, or <c>null</c> when the code is not mapped.</summary>
    public static string? For(string? code)
        => code is not null && ByCode.TryGetValue(code, out var message) ? message : null;

    /// <summary>
    /// The message to display for a problem: the first code this UI knows about, otherwise the
    /// caller's generic sentence.
    /// </summary>
    public static string Resolve(ApiProblem? problem, string fallback)
    {
        if (problem is null)
        {
            return fallback;
        }

        foreach (var code in problem.Codes)
        {
            if (For(code) is { } message)
            {
                return message;
            }
        }

        return fallback;
    }

    /// <summary>
    /// The message to display next to a form field, for one server-side validation error. Falls back to
    /// the server's own message: on a field, an English sentence still tells the user what to fix,
    /// where a generic one would not.
    /// </summary>
    public static string ForField(string field, string serverMessage)
        => For($"Validation.{field}") ?? serverMessage;
}
