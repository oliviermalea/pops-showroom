using AwesomeAssertions;
using ShowRoom.BuildingBlocks.Application.Validations;
using Xunit;

namespace ShowRoom.BuildingBlocks.Tests.Application.Validations;

public sealed class CurrenciesTests
{
    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("JPY")]
    public void IsSupported_returns_true_for_a_known_uppercase_iso_code(string code)
    {
        IsoCurrencies.IsSupported(code).Should().BeTrue();
    }

    [Theory]
    [InlineData("Eur")]   // wrong casing
    [InlineData("eur")]   // lower case
    [InlineData("ZZZ")]   // unknown code
    [InlineData("EU")]    // too short
    [InlineData("EURO")]  // too long
    [InlineData("123")]   // not letters
    [InlineData(" EUR")]  // surrounding whitespace
    [InlineData("")]
    [InlineData(null)]
    public void IsSupported_returns_false_for_malformed_or_unknown_codes(string? code)
    {
        IsoCurrencies.IsSupported(code).Should().BeFalse();
    }
}
