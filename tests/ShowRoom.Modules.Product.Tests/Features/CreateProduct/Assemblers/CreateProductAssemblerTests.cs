using AwesomeAssertions;
using ShowRoom.Modules.Product.Features.CreateProduct;
using Xunit;
using ProductAggregate = ShowRoom.Modules.Product.Domain.Product;

namespace ShowRoom.Modules.Product.Tests.Features.CreateProduct.Assemblers;

public sealed class CreateProductAssemblerTests
{
    [Fact]
    public void From_returns_the_product_public_id()
    {
        // Arrange
        var product = ProductAggregate.Create("Surf des mers", null, 349.90m, "EUR", DateTimeOffset.UtcNow).Value;

        // Act
        var sut = CreateProductAssembler.From(product);

        // Assert
        sut.Should().Be(product.PublicId);
        sut.Prefix.Should().Be("prd");
    }
}
