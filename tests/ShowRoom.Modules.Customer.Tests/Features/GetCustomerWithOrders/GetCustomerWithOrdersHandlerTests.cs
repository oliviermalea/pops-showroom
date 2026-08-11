using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.Modules.Order.Contracts.Messaging;
using ShowRoom.SharedKernel.Emails;
using Xunit;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Tests.Features.GetCustomerWithOrders;

public sealed class GetCustomerWithOrdersHandlerTests
{
    private static CustomersContext NewContext()
        => new(new DbContextOptionsBuilder<CustomersContext>()
            .UseInMemoryDatabase($"customers-{Guid.NewGuid():N}")
            .Options);

    private static async Task<CustomerAggregate> SeedAsync(CustomersContext context)
    {
        var customer = CustomerAggregate.Create(
            "Grace", "Hopper", Email.Create("grace@example.com").Value, phone: null, DateTimeOffset.UtcNow);
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private sealed class StubOrderHistory(OrdersForCustomerResponse response) : IOrderHistory
    {
        public Task<OrdersForCustomerResponse> ForCustomerAsync(string customerPublicId, CancellationToken cancellationToken = default)
            => Task.FromResult(response);
    }

    private sealed class ThrowingOrderHistory : IOrderHistory
    {
        public Task<OrdersForCustomerResponse> ForCustomerAsync(string customerPublicId, CancellationToken cancellationToken = default)
            => throw new TimeoutException("broker unavailable");
    }

    [Fact]
    public async Task Returns_customer_with_orders_when_the_bus_responds()
    {
        // Arrange
        await using var context = NewContext();
        var customer = await SeedAsync(context);
        var orders = new OrdersForCustomerResponse(
        [
            new CustomerOrderSummary("ord_" + new string('a', 32), "Pending", "EUR", DateTimeOffset.UtcNow, 74.90m,
            [
                new CustomerOrderLine("prd_" + new string('b', 32), "Voile d'écume", 1, 74.90m, 74.90m),
            ]),
        ]);
        var sut = new GetCustomerWithOrdersHandler(context, new StubOrderHistory(orders), NullLogger<GetCustomerWithOrdersHandler>.Instance);

        // Act
        var result = await sut.HandleAsync(new GetCustomerWithOrdersQuery(customer.PublicId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrdersAvailable.Should().BeTrue();
        result.Value.Orders.Should().ContainSingle();
        result.Value.PublicId.Should().Be(customer.PublicId);
    }

    [Fact]
    public async Task Degrades_gracefully_when_the_bus_fails()
    {
        // Arrange
        await using var context = NewContext();
        var customer = await SeedAsync(context);
        var sut = new GetCustomerWithOrdersHandler(context, new ThrowingOrderHistory(), NullLogger<GetCustomerWithOrdersHandler>.Instance);

        // Act
        var result = await sut.HandleAsync(new GetCustomerWithOrdersQuery(customer.PublicId));

        // Assert — customer still returned, orders empty and flagged unavailable
        result.IsSuccess.Should().BeTrue();
        result.Value.OrdersAvailable.Should().BeFalse();
        result.Value.Orders.Should().BeEmpty();
        result.Value.PublicId.Should().Be(customer.PublicId);
    }

    [Fact]
    public async Task Returns_not_found_when_the_customer_does_not_exist()
    {
        // Arrange
        await using var context = NewContext();
        var unknown = PublicIdFactory.ForCustomer().Value;
        var sut = new GetCustomerWithOrdersHandler(
            context,
            new StubOrderHistory(new OrdersForCustomerResponse([])),
            NullLogger<GetCustomerWithOrdersHandler>.Instance);

        // Act
        var result = await sut.HandleAsync(new GetCustomerWithOrdersQuery(unknown));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.FirstError.Category.Should().Be(ErrorCategory.NotFound);
    }
}
