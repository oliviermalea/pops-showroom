using AwesomeAssertions;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Order.Domain;
using ShowRoom.Modules.Order.Features.GetOrders;
using Xunit;
using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Tests.Features.GetOrders.Assemblers;

public sealed class GetOrdersAssemblerTests
{
    [Fact]
    public void ToSummary_projects_totals_and_item_count_without_line_detail()
    {
        // Arrange
        var customerPublicId = PublicIdFactory.ForCustomer().Value;
        var createdAt = DateTimeOffset.UtcNow;

        var order = OrderAggregate.Create(
            customerPublicId,
            "EUR",
            [
                new OrderLineDraft(PublicIdFactory.ForContentNode().Value, "Widget", 2, 10m),
                new OrderLineDraft(PublicIdFactory.ForContentNode().Value, "Gadget", 4, 2.5m),
            ],
            createdAt).Value;

        // Act
        var sut = GetOrdersAssembler.ToSummary(order);

        // Assert
        sut.PublicId.Should().Be(order.PublicId);
        sut.CustomerPublicId.Should().Be(customerPublicId.Value);
        sut.Status.Should().Be("Pending");
        sut.Currency.Should().Be("EUR");
        sut.TotalAmount.Should().Be(30m);
        sut.ItemCount.Should().Be(2);
        sut.CreatedAt.Should().Be(createdAt);
    }
}
