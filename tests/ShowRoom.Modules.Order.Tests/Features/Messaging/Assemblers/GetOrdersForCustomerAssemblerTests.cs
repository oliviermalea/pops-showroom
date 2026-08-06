using AwesomeAssertions;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Order.Domain;
using ShowRoom.Modules.Order.Features.Messaging;
using Xunit;
using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Tests.Features.Messaging.Assemblers;

public sealed class GetOrdersForCustomerAssemblerTests
{
    [Fact]
    public void From_maps_each_order_to_a_public_summary()
    {
        // Arrange
        var customerPublicId = PublicIdFactory.ForCustomer().Value;
        var order = OrderAggregate.Create(
            customerPublicId,
            "EUR",
            [
                new OrderLineDraft(PublicIdFactory.ForProduct().Value, "Surf des mers", 2, 10m),
                new OrderLineDraft(PublicIdFactory.ForProduct().Value, "Kayak", 1, 5.5m),
            ],
            DateTimeOffset.UtcNow).Value;

        // Act
        var sut = GetOrdersForCustomerAssembler.From([order]);

        // Assert
        sut.Orders.Should().ContainSingle();
        var summary = sut.Orders.Single();
        summary.OrderPublicId.Should().Be(order.PublicId.Value);
        summary.Status.Should().Be("Pending");
        summary.Currency.Should().Be("EUR");
        summary.TotalAmount.Should().Be(25.5m);
        summary.ItemCount.Should().Be(2);
    }

    [Fact]
    public void From_maps_an_empty_collection()
    {
        var sut = GetOrdersForCustomerAssembler.From([]);

        sut.Orders.Should().BeEmpty();
    }
}
