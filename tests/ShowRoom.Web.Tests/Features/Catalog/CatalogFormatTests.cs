using System.Globalization;
using AwesomeAssertions;
using ShowRoom.Web.Client.Features.Catalog;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Catalog;

/// <summary>
/// Le formatage est partagé par la grille et la fiche : une même donnée doit s'afficher de la même
/// façon des deux côtés, sinon l'écran devient l'autorité sur la présentation.
/// </summary>
public sealed class CatalogFormatTests
{
    [Fact]
    public void A_price_carries_its_currency_code()
    {
        // Arrange
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        try
        {
            // Act
            var sut = CatalogFormat.Money(1180m, "eur");

            // Assert — le code ISO est normalisé en majuscules, le nombre suit la culture de l'UI.
            sut.Should().EndWith("EUR");
            sut.Should().Contain("180");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void A_price_without_currency_shows_the_amount_alone()
    {
        // Act
        var sut = CatalogFormat.Money(10m, null);

        // Assert
        sut.Should().NotContain("EUR");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_value_becomes_the_placeholder(string? value)
    {
        // Act
        var sut = CatalogFormat.OrPlaceholder(value);

        // Assert
        sut.Should().Be(CatalogFormat.Placeholder);
    }

    [Theory]
    [InlineData("Available", true)]
    [InlineData("available", true)]
    [InlineData("Discontinued", false)]
    [InlineData(null, false)]
    public void Availability_is_derived_from_the_status(string? status, bool expected)
    {
        // Act
        var sut = CatalogFormat.IsAvailable(status);

        // Assert
        sut.Should().Be(expected);
    }
}
