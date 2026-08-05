using AwesomeAssertions;
using ShowRoom.Modules.Product.Features.GetProducts;
using Xunit;
using ProductAggregate = ShowRoom.Modules.Product.Domain.Product;

namespace ShowRoom.Modules.Product.Tests.Features.GetProducts.Assemblers;

public sealed class GetProductsAssemblerTests
{
    [Fact]
    public void ToSummary_projects_the_compact_view()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var product = ProductAggregate.Create("Surf des mers", "desc", 349.90m, "EUR", createdAt).Value;

        // Act
        var sut = GetProductsAssembler.ToSummary(product);

        // Assert
        sut.PublicId.Should().Be(product.PublicId);
        sut.Name.Should().Be("Surf des mers");
        sut.Price.Should().Be(349.90m);
        sut.Currency.Should().Be("EUR");
        sut.Status.Should().Be("Available");
        sut.CreatedAt.Should().Be(createdAt);
    }
}
