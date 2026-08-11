using Ardalis.SmartEnum;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.SharedKernel.Currencies;

/// <summary>
/// Supported ISO 4217 currency, modelled as a <see cref="SmartEnum{TEnum}"/> (never a raw <c>enum</c>).
/// <see cref="SmartEnum{TEnum}.Name"/> is the 3-letter alphabetic code (e.g. <c>"EUR"</c>, exposed as
/// <see cref="Code"/>) and <see cref="SmartEnum{TEnum}.Value"/> is the ISO 4217 numeric code (e.g. 978).
/// The set here is the single source of truth — persisted as its <see cref="Code"/> via an EF value
/// converter, validated at the API boundary with <see cref="IsValidCode"/>, and resolved in the domain
/// with <see cref="FromCode"/> / <see cref="FromCodeOrDefault"/> (no lookup table — this lives in code).
/// </summary>
public sealed class Currency : SmartEnum<Currency>
{
    public static readonly Currency Eur = new("EUR", 978);
    public static readonly Currency Usd = new("USD", 840);
    public static readonly Currency Gbp = new("GBP", 826);
    public static readonly Currency Jpy = new("JPY", 392);
    public static readonly Currency Chf = new("CHF", 756);
    public static readonly Currency Cad = new("CAD", 124);
    public static readonly Currency Aud = new("AUD", 36);
    public static readonly Currency Cny = new("CNY", 156);
    public static readonly Currency Sek = new("SEK", 752);
    public static readonly Currency Nok = new("NOK", 578);

    /// <summary>
    /// Default currency used when a caller omits the currency (EUR). A property, NOT a static field:
    /// SmartEnum reflects over static fields to build its set, so a second field pointing at EUR would
    /// register the code twice ("An item with the same key has already been added").
    /// </summary>
    public static Currency Default => Eur;

    private Currency(string name, int value) : base(name, value)
    {
    }

    /// <summary>The ISO 4217 alphabetic code (alias of <see cref="SmartEnum{TEnum}.Name"/>, e.g. <c>"EUR"</c>).</summary>
    public string Code => Name;

    /// <summary>
    /// Strict boundary check: <c>true</c> only for an exact, uppercase supported code. Rejects malformed
    /// casing (<c>"Eur"</c>) and unknown codes — used by API validators.
    /// </summary>
    public static bool IsValidCode(string? code) =>
        code is not null && TryFromName(code, out _);

    /// <summary>
    /// Tolerant domain resolution: trims and upper-cases before matching, returning an <see cref="Error"/>
    /// (never throwing) for an unknown or blank code.
    /// </summary>
    public static Result<Currency> FromCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)
            || !TryFromName(code.Trim().ToUpperInvariant(), out var currency))
        {
            return CurrencyErrors.Invalid(code);
        }

        return Result<Currency>.Success(currency);
    }

    /// <summary>Resolves the code, or falls back to <see cref="Default"/> when none is provided.</summary>
    public static Result<Currency> FromCodeOrDefault(string? code)
        => string.IsNullOrWhiteSpace(code) ? Result<Currency>.Success(Default) : FromCode(code);

    public static implicit operator string(Currency currency) => currency.Name;
}
