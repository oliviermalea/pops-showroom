using AwesomeAssertions;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Order.Features.CreateOrder;
using Xunit;
using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Tests.Features.CreateOrder.Assemblers;

public sealed class CreateOrderAssemblerTests
{
    private static string NewCustomerId() => PublicIdFactory.ForCustomer().Value.Value;
    private static string NewProductId() => PublicIdFactory.ForContentNode().Value.Value;

    [Fact]
    public void ToDrafts_maps_every_command_line()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerPublicId = NewCustomerId(),
            Currency = "EUR",
            Lines =
            [
                new CreateOrderLine { ProductPublicId = NewProductId(), ProductName = "A", Quantity = 2, UnitPrice = 5m },
                new CreateOrderLine { ProductPublicId = NewProductId(), ProductName = "B", Quantity = 3, UnitPrice = 4m },
            ],
        };

        // Act
        var drafts = CreateOrderAssembler.ToDrafts(command).ToList();

        // Assert
        drafts.Should().HaveCount(2);
        drafts[0].ProductName.Should().Be("A");
        drafts[0].Quantity.Should().Be(2);
        drafts[0].UnitPrice.Should().Be(5m);
        drafts[0].ProductPublicId.Value.Should().Be(command.Lines[0].ProductPublicId);
        drafts[1].ProductName.Should().Be("B");
    }

    [Fact]
    public void From_returns_the_order_public_id()
    {
        // Arrange
        var order = OrderAggregate.Create(
            PublicIdFactory.ForCustomer().Value,
            "EUR",
            [new ShowRoom.Modules.Order.Domain.OrderLineDraft(PublicIdFactory.ForContentNode().Value, "Widget", 1, 10m)],
            DateTimeOffset.UtcNow).Value;

        // Act
        var sut = CreateOrderAssembler.From(order);

        // Assert
        sut.Should().Be(order.PublicId);
        sut.Prefix.Should().Be("ord");
    }
}
