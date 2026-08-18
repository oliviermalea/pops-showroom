using System.Globalization;

namespace ShowRoom.Web.Features.Customer;

/// <summary>
/// Presentation rules shared by every Customer screen, so a customer reads the same way in the list
/// and in the detail view.
/// </summary>
public static class CustomerFormat
{
    private const string ActiveStatus = "Active";

    /// <summary>Placeholder shown for an optional value the customer did not provide.</summary>
    public const string NotProvided = "—";

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("fr-FR");

    /// <summary>
    /// Formats a date in French. The instant is rendered as sent by the backend (offset preserved) so
    /// the same payload always yields the same text, whatever the server timezone.
    /// </summary>
    public static string Date(DateTimeOffset value)
        => value.ToString("dd MMMM yyyy", DisplayCulture);

    /// <summary>Returns the value, or the placeholder when it is absent.</summary>
    public static string OrPlaceholder(string? value)
        => string.IsNullOrWhiteSpace(value) ? NotProvided : value;

    /// <summary>Falls back on "first last" when the backend display name is empty.</summary>
    public static string DisplayName(string? displayName, string firstName, string lastName)
        => string.IsNullOrWhiteSpace(displayName) ? $"{firstName} {lastName}".Trim() : displayName;

    /// <summary>Derives the active flag from the status value.</summary>
    public static bool IsActive(string? status)
        => string.Equals(status, ActiveStatus, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Formats an amount in French with its ISO 4217 code (<c>1 234,50 EUR</c>). The code is kept
    /// rather than mapped to a symbol: the backend models currencies as ISO codes, and inventing a
    /// symbol table on the front would be a second source of truth.
    /// </summary>
    public static string Money(decimal amount, string? currency)
    {
        var formatted = amount.ToString("N2", DisplayCulture);

        return string.IsNullOrWhiteSpace(currency) ? formatted : $"{formatted} {currency.ToUpperInvariant()}";
    }

    /// <summary>Formats a quantity of items ("1 article", "3 articles").</summary>
    public static string ItemCount(int count)
        => count <= 1 ? $"{count} article" : $"{count} articles";
}
