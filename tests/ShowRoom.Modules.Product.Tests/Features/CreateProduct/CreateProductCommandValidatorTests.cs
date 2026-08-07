using AwesomeAssertions;
using ShowRoom.Modules.Product.Features.CreateProduct;
using Xunit;

namespace ShowRoom.Modules.Product.Tests.Features.CreateProduct;

public sealed class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _sut = new();

    private static CreateProductCommand ValidCommand() => new()
    {
        Name = "Surf des mers",
        Description = "Une planche légendaire",
        Price = 349.90m,
        Currency = "EUR",
    };

    [Fact]
    public void Valid_command_passes_validation()
    {
        _sut.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Fails_when_name_is_empty()
    {
        _sut.Validate(ValidCommand() with { Name = "" }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Fails_when_price_is_negative()
    {
        _sut.Validate(ValidCommand() with { Price = -1m }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Fails_when_currency_is_not_three_letters()
    {
        _sut.Validate(ValidCommand() with { Currency = "EU" }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Eur")]  // wrong casing
    [InlineData("eur")]  // lower case
    [InlineData("ZZZ")]  // unknown ISO code
    [InlineData("123")]  // not letters
    [InlineData("EURO")] // too long
    public void Fails_when_currency_is_not_a_supported_iso_code(string currency)
    {
        _sut.Validate(ValidCommand() with { Currency = currency }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("JPY")]
    public void Passes_for_a_supported_currency(string currency)
    {
        _sut.Validate(ValidCommand() with { Currency = currency }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Passes_when_currency_is_omitted_and_defaulted_downstream(string? currency)
    {
        _sut.Validate(ValidCommand() with { Currency = currency }).IsValid.Should().BeTrue();
    }
}
