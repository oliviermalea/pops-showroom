using AwesomeAssertions;
using ShowRoom.Modules.Product.Domain;
using ShowRoom.SharedKernel.Currencies;
using Xunit;
using ProductAggregate = ShowRoom.Modules.Product.Domain.Product;

namespace ShowRoom.Modules.Product.Tests.Domain;

public sealed class ProductTests
{
    [Fact]
    public void Create_returns_an_available_product_with_a_prefixed_public_id()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var sut = ProductAggregate.Create("Surf des mers", "Une planche légendaire", 349.90m, "eur", createdAt);

        // Assert
        sut.IsSuccess.Should().BeTrue();
        sut.Value.PublicId.Prefix.Should().Be("prd");
        sut.Value.Name.Should().Be("Surf des mers");
        sut.Value.Description.Should().Be("Une planche légendaire");
        sut.Value.Price.Should().Be(349.90m);
        sut.Value.Currency.Value.Should().Be("EUR");
        sut.Value.Status.Should().Be(ProductStatus.Available);
        sut.Value.CreatedAt.Should().Be(createdAt);
        sut.Value.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_defaults_currency_to_eur_when_omitted()
    {
        var sut = ProductAggregate.Create("Kayak", description: null, 10m, currency: null, DateTimeOffset.UtcNow);

        sut.IsSuccess.Should().BeTrue();
        sut.Value.Currency.Should().Be(Currency.Default);
        sut.Value.Description.Should().BeNull();
    }

    [Fact]
    public void Create_fails_when_name_is_blank()
    {
        var sut = ProductAggregate.Create("   ", null, 10m, "EUR", DateTimeOffset.UtcNow);

        sut.IsFailure.Should().BeTrue();
        sut.Errors.Should().Contain(ProductErrors.NameRequired);
    }

    [Fact]
    public void Create_fails_when_price_is_negative()
    {
        var sut = ProductAggregate.Create("Voile", null, -1m, "EUR", DateTimeOffset.UtcNow);

        sut.IsFailure.Should().BeTrue();
        sut.Errors.Should().Contain(ProductErrors.InvalidPrice);
    }
}
