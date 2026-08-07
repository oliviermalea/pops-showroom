using ShowRoom.BuildingBlocks.Application.Validations;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.SharedKernel.Currencies;

/// <summary>
/// Validated ISO 4217 currency value object. Normalised to a trimmed, uppercase 3-letter code and
/// validated against the supported set (<see cref="Currencies"/>). Creation failures are returned as an
/// <see cref="Error"/> rather than thrown — invalid input never produces a <see cref="Currency"/>.
/// Adapted from the PerpetualOps <c>Money</c> shared-kernel value object, narrowed to the currency
/// concept and hardened with the curated ISO 4217 set.
/// </summary>
public sealed record Currency
{
    /// <summary>The system default currency (EUR), used when a caller omits the currency.</summary>
    public static readonly Currency Default = new("EUR");

    private Currency(string value) => Value = value;

    /// <summary>The canonical uppercase 3-letter ISO 4217 code (e.g. <c>"EUR"</c>).</summary>
    public string Value { get; }

    /// <summary>Creates a currency from a raw code, failing when it is not a supported ISO 4217 code.</summary>
    public static Result<Currency> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CurrencyErrors.Invalid(value);
        }

        var normalized = Normalize(value);

        if (!IsoCurrencies.IsSupported(normalized))
        {
            return CurrencyErrors.Invalid(value);
        }

        return Result<Currency>.Success(new Currency(normalized));
    }

    /// <summary>Creates the currency, or falls back to <see cref="Default"/> when none is provided.</summary>
    public static Result<Currency> CreateOrDefault(string? value)
        => string.IsNullOrWhiteSpace(value) ? Result<Currency>.Success(Default) : Create(value);

    /// <summary>Returns <c>true</c> when <paramref name="value"/> normalises to a supported ISO 4217 code.</summary>
    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && IsoCurrencies.IsSupported(Normalize(value));

    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    public override string ToString() => Value;

    public static implicit operator string(Currency currency) => currency.Value;
}
