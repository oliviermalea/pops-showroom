using FluentValidation;

namespace ShowRoom.BuildingBlocks.Application.Validations;

/// <summary>
/// Reusable FluentValidation rule for ISO 4217 currency codes, so every module validates currency the
/// same way (uniform ubiquitous language and error message across Order, Product, …).
/// </summary>
public static class CurrencyValidationExtensions
{
    /// <summary>
    /// Fails unless the value is a supported ISO 4217 code (exactly three uppercase letters from the
    /// curated <see cref="Currencies"/> set). Combine with <c>.When(x =&gt; !string.IsNullOrWhiteSpace(...))</c>
    /// when the currency is optional and defaulted downstream.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeSupportedCurrency<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
        => ruleBuilder
            .Must(IsoCurrencies.IsSupported)
            .WithMessage("Currency must be a supported 3-letter uppercase ISO 4217 code (e.g. \"EUR\").");
}
