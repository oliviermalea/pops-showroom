using AwesomeAssertions;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Order.Domain;
using ShowRoom.Modules.Order.Features.GetOrderByPublicId;
using Xunit;
using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Tests.Features.GetOrderByPublicId.Assemblers;

public sealed class GetOrderByPublicIdAssemblerTests
{
    [Fact]
    public void From_maps_all_properties_and_lines_with_computed_totals()
    {
        // Arrange
        var customerPublicId = PublicIdFactory.ForCustomer().Value;
        var productA = PublicIdFactory.ForContentNode().Value;
        var productB = PublicIdFactory.ForContentNode().Value;
        var createdAt = DateTimeOffset.UtcNow;

        var order = OrderAggregate.Create(
            customerPublicId,
            "EUR",
            [
                new OrderLineDraft(productA, "Widget", 2, 10m),
                new OrderLineDraft(productB, "Gadget", 1, 5.5m),
            ],
            createdAt).Value;

        // Act
        var sut = GetOrderByPublicIdAssembler.From(order);

        // Assert
        sut.PublicId.Should().Be(order.PublicId);
        sut.CustomerPublicId.Should().Be(customerPublicId.Value);
        sut.Currency.Should().Be("EUR");
        sut.Status.Should().Be("Pending");
        sut.TotalAmount.Should().Be(25.5m);
        sut.OrderDate.Should().Be(createdAt);
        sut.Lines.Should().HaveCount(2);

        var firstLine = sut.Lines.First();
        firstLine.ProductPublicId.Should().Be(productA.Value);
        firstLine.ProductName.Should().Be("Widget");
        firstLine.Quantity.Should().Be(2);
        firstLine.UnitPrice.Should().Be(10m);
        firstLine.LineTotal.Should().Be(20m);
    }
}
