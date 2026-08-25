using System.Globalization;

namespace ShowRoom.Web.Client.Features.Catalog;

/// <summary>
/// Presentation rules shared by every catalogue screen: a grid row and a detail card must show the same
/// data the same way.
/// </summary>
public static class CatalogFormat
{
    /// <summary>Placeholder for a value the API did not provide.</summary>
    public const string Placeholder = "—";

    /// <summary>Formats a price with its ISO 4217 code, in the UI culture.</summary>
    public static string Money(decimal amount, string? currency)
        => string.IsNullOrWhiteSpace(currency)
            ? amount.ToString("N2", CultureInfo.CurrentCulture)
            : $"{amount.ToString("N2", CultureInfo.CurrentCulture)} {currency.ToUpperInvariant()}";

    /// <summary>Returns the value, or the placeholder when it carries nothing.</summary>
    public static string OrPlaceholder(string? value)
        => string.IsNullOrWhiteSpace(value) ? Placeholder : value;

    /// <summary>Whether the product is offered for sale — derived from the status, never hard-coded in a screen.</summary>
    public static bool IsAvailable(string? status)
        => string.Equals(status, "Available", StringComparison.OrdinalIgnoreCase);
}
