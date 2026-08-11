using AwesomeAssertions;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Product.Domain;
using ShowRoom.Modules.Product.Features.GetProductByPublicId;
using ShowRoom.SharedKernel.Currencies;
using Xunit;
using ProductAggregate = ShowRoom.Modules.Product.Domain.Product;

namespace ShowRoom.Modules.Product.Tests.Features.GetProductByPublicId.Assemblers;

public sealed class GetProductByPublicIdAssemblerTests
{
    [Fact]
    public void From_maps_all_properties_for_a_complete_product()
    {
        // Arrange
        var publicId = PublicIdFactory.ForProduct().Value;
        var createdAt = DateTimeOffset.UtcNow.AddDays(-3);
        var updatedAt = DateTimeOffset.UtcNow;

        var product = ProductAggregate.Restore(
            ProductId.FromGuid(Guid.CreateVersion7()),
            publicId,
            "Surf des mers",
            "Une planche légendaire",
            349.90m,
            Currency.Create("EUR").Value,
            ProductStatus.Discontinued,
            createdAt,
            updatedAt);

        // Act
        var sut = GetProductByPublicIdAssembler.From(product);

        // Assert
        sut.PublicId.Should().Be(publicId);
        sut.Name.Should().Be("Surf des mers");
        sut.Description.Should().Be("Une planche légendaire");
        sut.Price.Should().Be(349.90m);
        sut.Currency.Should().Be("EUR");
        sut.Status.Should().Be("Discontinued");
    }

    [Fact]
    public void From_maps_null_description_when_absent()
    {
        var product = ProductAggregate.Create("Kayak", null, 10m, "EUR", DateTimeOffset.UtcNow).Value;

        var sut = GetProductByPublicIdAssembler.From(product);

        sut.Description.Should().BeNull();
        sut.Status.Should().Be("Available");
    }
}
