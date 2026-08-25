using AwesomeAssertions;
using ShowRoom.Web.Client.Features.Catalog.ProductList;
using ShowRoom.Web.Client.Infrastructure.Api.Refit.Catalog.Models;
using ShowRoom.Web.Shared.Api.Models;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Catalog;

public sealed class ProductListMapperTests
{
    [Fact]
    public void It_maps_a_page_and_derives_its_navigation_state()
    {
        // Arrange
        var response = new PagedResponse<ProductSummaryResponse>(
            Items: [new ProductSummaryResponse("prd_1", "Chaise", 345.5m, "EUR", "Available")],
            Page: 2,
            PageSize: 12,
            TotalItems: 18,
            TotalPages: 2);

        // Act
        var sut = ProductListMapper.FromApi(response);

        // Assert
        sut.Items.Should().ContainSingle();
        sut.Items[0].Price.Should().Contain("EUR");
        sut.Items[0].IsAvailable.Should().BeTrue();
        sut.HasPrevious.Should().BeTrue();
        sut.HasNext.Should().BeFalse("la page 2 sur 2 est la dernière");
        sut.FirstItemIndex.Should().Be(13);
        sut.LastItemIndex.Should().Be(13);
    }

    [Fact]
    public void An_empty_page_reports_no_range()
    {
        // Arrange
        var response = new PagedResponse<ProductSummaryResponse>([], 1, 12, 0, 0);

        // Act
        var sut = ProductListMapper.FromApi(response);

        // Assert
        sut.IsEmpty.Should().BeTrue();
        sut.FirstItemIndex.Should().Be(0);
        sut.LastItemIndex.Should().Be(0);
        sut.HasPrevious.Should().BeFalse();
        sut.HasNext.Should().BeFalse();
    }

    [Fact]
    public void A_null_item_collection_is_tolerated()
    {
        // Arrange — le contrat autorise l'absence du tableau ; l'écran ne doit pas exploser pour ça.
        var response = new PagedResponse<ProductSummaryResponse>(null!, 1, 12, 0, 0);

        // Act
        var sut = ProductListMapper.FromApi(response);

        // Assert
        sut.Items.Should().BeEmpty();
    }
}
