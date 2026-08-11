using AwesomeAssertions;
using ShowRoom.SharedKernel.Currencies;
using Xunit;

namespace ShowRoom.Modules.Product.Tests.Domain;

public sealed class CurrencyTests
{
    [Fact]
    public void Default_is_eur()
    {
        Currency.Default.Should().Be(Currency.Eur);
        Currency.Default.Code.Should().Be("EUR");
        Currency.Default.Value.Should().Be(978); // ISO 4217 numeric code
    }

    [Fact]
    public void List_exposes_the_supported_currencies()
    {
        Currency.List.Should().HaveCount(10);
        Currency.List.Select(c => c.Code).Should().Contain(["EUR", "USD", "GBP", "JPY", "NOK"]);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    public void IsValidCode_is_strict_and_accepts_exact_uppercase_codes(string code)
    {
        Currency.IsValidCode(code).Should().BeTrue();
    }

    [Theory]
    [InlineData("eur")]   // wrong casing rejected at the boundary
    [InlineData("Eur")]
    [InlineData("ZZZ")]   // unknown
    [InlineData("EU")]    // too short
    [InlineData("EURO")]  // too long
    [InlineData(" EUR")]  // surrounding whitespace
    [InlineData("")]
    [InlineData(null)]
    public void IsValidCode_rejects_malformed_or_unknown_codes(string? code)
    {
        Currency.IsValidCode(code).Should().BeFalse();
    }

    [Theory]
    [InlineData("eur")]
    [InlineData("  Eur  ")]
    public void FromCode_is_tolerant_and_normalises_casing_and_whitespace(string input)
    {
        var sut = Currency.FromCode(input);

        sut.IsSuccess.Should().BeTrue();
        sut.Value.Should().Be(Currency.Eur);
        sut.Value.Code.Should().Be("EUR");
    }

    [Theory]
    [InlineData("ZZZ")]
    [InlineData("12")]
    [InlineData("")]
    [InlineData(null)]
    public void FromCode_fails_for_unknown_or_blank_codes(string? input)
    {
        var sut = Currency.FromCode(input);

        sut.IsFailure.Should().BeTrue();
        sut.FirstError.Code.Should().Be("Currency.Invalid");
    }

    [Fact]
    public void FromCodeOrDefault_falls_back_to_eur_when_omitted()
    {
        Currency.FromCodeOrDefault(null).Value.Should().Be(Currency.Default);
        Currency.FromCodeOrDefault("   ").Value.Should().Be(Currency.Default);
        Currency.FromCodeOrDefault("USD").Value.Should().Be(Currency.Usd);
    }

    [Fact]
    public void Implicit_string_and_code_expose_the_alpha_code()
    {
        string asString = Currency.Gbp;
        asString.Should().Be("GBP");
        Currency.Gbp.Code.Should().Be("GBP");
    }
}
