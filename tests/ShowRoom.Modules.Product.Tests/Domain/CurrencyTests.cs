using AwesomeAssertions;
using ShowRoom.SharedKernel.Currencies;
using Xunit;

namespace ShowRoom.Modules.Product.Tests.Domain;

public sealed class CurrencyTests
{
    [Fact]
    public void Create_succeeds_and_keeps_a_supported_uppercase_code()
    {
        var sut = Currency.Create("USD");

        sut.IsSuccess.Should().BeTrue();
        sut.Value.Value.Should().Be("USD");
    }

    [Theory]
    [InlineData("eur")]
    [InlineData("  Eur  ")]
    public void Create_normalises_casing_and_whitespace(string input)
    {
        var sut = Currency.Create(input);

        sut.IsSuccess.Should().BeTrue();
        sut.Value.Value.Should().Be("EUR");
    }

    [Theory]
    [InlineData("ZZZ")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("12")]
    [InlineData(null)]
    [InlineData("")]
    public void Create_fails_for_unknown_or_malformed_codes(string? input)
    {
        var sut = Currency.Create(input);

        sut.IsFailure.Should().BeTrue();
        sut.FirstError.Code.Should().Be("Currency.Invalid");
    }

    [Fact]
    public void CreateOrDefault_falls_back_to_eur_when_omitted()
    {
        Currency.CreateOrDefault(null).Value.Should().Be(Currency.Default);
        Currency.CreateOrDefault("   ").Value.Should().Be(Currency.Default);
        Currency.Default.Value.Should().Be("EUR");
    }

    [Fact]
    public void Implicit_string_and_ToString_expose_the_code()
    {
        var sut = Currency.Create("GBP").Value;

        string asString = sut;
        asString.Should().Be("GBP");
        sut.ToString().Should().Be("GBP");
    }
}
