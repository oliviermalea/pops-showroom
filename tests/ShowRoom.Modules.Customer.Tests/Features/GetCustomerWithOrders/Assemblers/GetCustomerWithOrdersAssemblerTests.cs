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
        var orderDate = DateTimeOffset.UtcNow.AddDays(-2);
        var orders = new[]
        {
            new CustomerOrderSummary("ord_" + new string('a', 32), "Pending", "EUR", orderDate, 199.90m,
            [
                new CustomerOrderLine("prd_" + new string('b', 32), "Voile d'écume", 1, 74.90m, 74.90m),
                new CustomerOrderLine("prd_" + new string('c', 32), "Planche des cimes", 1, 125.00m, 125.00m),
            ]),
        };

        // Act
        var sut = GetCustomerWithOrdersAssembler.From(customer, orders, ordersAvailable: true);

        // Assert
        sut.PublicId.Should().Be(customer.PublicId);
        sut.DisplayName.Should().Be("Grace Hopper");
        sut.Email.Should().Be("grace@example.com");
        sut.RegisteredOn.Should().Be(customer.CreatedAt);
        sut.OrdersAvailable.Should().BeTrue();
        sut.Orders.Should().ContainSingle();

        var order = sut.Orders.Single();
        order.OrderPublicId.Should().Be(orders[0].OrderPublicId);
        order.TotalAmount.Should().Be(199.90m);
        order.OrderDate.Should().Be(orderDate);
        order.Lines.Should().HaveCount(2);
        order.Lines.Should().Contain(line =>
            line.ProductName == "Voile d'écume" && line.Quantity == 1 && line.LineTotal == 74.90m);
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
