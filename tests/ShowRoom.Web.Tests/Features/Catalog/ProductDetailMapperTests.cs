using AwesomeAssertions;
using ShowRoom.Web.Client.Features.Catalog;
using ShowRoom.Web.Client.Features.Catalog.ProductDetail;
using ShowRoom.Web.Client.Infrastructure.Api.Refit.Catalog.Models;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Catalog;

public sealed class ProductDetailMapperTests
{
    [Fact]
    public void It_produces_a_presentable_view_model()
    {
        // Arrange
        var response = new ProductResponse("prd_1", "Bureau Compas", "Plateau chêne", 1180m, "EUR", "Available");

        // Act
        var sut = ProductDetailMapper.FromApi(response);

        // Assert
        sut.Name.Should().Be("Bureau Compas");
        sut.Description.Should().Be("Plateau chêne");
        sut.Price.Should().Contain("EUR");
        sut.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void A_missing_description_becomes_the_placeholder()
    {
        // Arrange — la description est facultative côté contrat.
        var response = new ProductResponse("prd_1", "Lampe", null, 189m, "EUR", "Discontinued");

        // Act
        var sut = ProductDetailMapper.FromApi(response);

        // Assert
        sut.Description.Should().Be(CatalogFormat.Placeholder);
        sut.IsAvailable.Should().BeFalse();
    }
}
