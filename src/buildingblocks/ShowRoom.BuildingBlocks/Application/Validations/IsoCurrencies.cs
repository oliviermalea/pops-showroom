namespace ShowRoom.BuildingBlocks.Application.Validations;

/// <summary>
/// Supported ISO 4217 alphabetic currency codes. Membership is checked with an ordinal (case-sensitive)
/// comparison, so a code is accepted only when it is exactly three UPPERCASE letters belonging to the
/// curated set — this rejects malformed casing (e.g. <c>"Eur"</c>) and unknown codes (e.g. <c>"ZZZ"</c>)
/// in a single check. The set is a deterministic, platform-independent subset (no dependency on ICU /
/// <see cref="System.Globalization.RegionInfo"/>); extend it here when a new currency must be accepted.
/// </summary>
public static class IsoCurrencies
{
    private static readonly HashSet<string> Codes = new(StringComparer.Ordinal)
    {
        "EUR", "USD", "GBP", "JPY", "CHF", "CAD", "AUD", "NZD", "CNY", "HKD",
        "SGD", "SEK", "NOK", "DKK", "PLN", "CZK", "HUF", "RON", "BGN", "ISK",
        "RUB", "TRY", "UAH", "INR", "BRL", "MXN", "ZAR", "AED", "SAR", "QAR",
        "KWD", "BHD", "OMR", "ILS", "EGP", "MAD", "TND", "NGN", "KES", "GHS",
        "THB", "MYR", "IDR", "PHP", "VND", "KRW", "TWD", "PKR", "BDT", "LKR",
        "CLP", "COP", "ARS", "PEN", "UYU", "KZT",
    };

    /// <summary>Returns <c>true</c> when <paramref name="code"/> is a supported ISO 4217 currency code.</summary>
    public static bool IsSupported(string? code) => code is not null && Codes.Contains(code);
}
