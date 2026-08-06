using AwesomeAssertions;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Customer.Domain;
using ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;
using ShowRoom.Modules.Order.Contracts.Messaging;
using ShowRoom.SharedKernel.Emails;
using Xunit;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Tests.Features.GetCustomerWithOrders.Assemblers;

public sealed class GetCustomerWithOrdersAssemblerTests
{
    private static CustomerAggregate NewCustomer()
        => CustomerAggregate.Create("Grace", "Hopper", Email.Create("grace@example.com").Value, phone: null, DateTimeOffset.UtcNow);

    [Fact]
    public void From_maps_customer_and_orders_and_flags_availability_true()
    {
        // Arrange
        var customer = NewCustomer();
        var orders = new[]
        {
            new CustomerOrderSummary("ord_" + new string('a', 32), "Pending", "EUR", 25.5m, 2, DateTimeOffset.UtcNow),
        };

        // Act
        var sut = GetCustomerWithOrdersAssembler.From(customer, orders, ordersAvailable: true);

        // Assert
        sut.PublicId.Should().Be(customer.PublicId);
        sut.DisplayName.Should().Be("Grace Hopper");
        sut.Email.Should().Be("grace@example.com");
        sut.OrdersAvailable.Should().BeTrue();
        sut.Orders.Should().ContainSingle();
        sut.Orders.Single().OrderPublicId.Should().Be(orders[0].OrderPublicId);
        sut.Orders.Single().TotalAmount.Should().Be(25.5m);
        sut.Orders.Single().ItemCount.Should().Be(2);
    }

    [Fact]
    public void From_maps_empty_orders_and_flags_availability_false_when_degraded()
    {
        var customer = NewCustomer();

        var sut = GetCustomerWithOrdersAssembler.From(customer, [], ordersAvailable: false);

        sut.OrdersAvailable.Should().BeFalse();
        sut.Orders.Should().BeEmpty();
        sut.PublicId.Should().Be(customer.PublicId);
    }
}
