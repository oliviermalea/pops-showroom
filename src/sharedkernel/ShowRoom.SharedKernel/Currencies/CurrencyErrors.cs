using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.SharedKernel.Currencies;

public static class CurrencyErrors
{
    public static Error Invalid(string? value) =>
        Error.Validation(
            "Currency.Invalid",
            $"The currency is invalid: '{value}'. Expected a supported 3-letter uppercase ISO 4217 code (e.g. \"EUR\").");
}
