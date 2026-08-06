namespace ShowRoom.Modules.Customer.IntegrationTests.GetCustomerWithOrders;

using System.Collections.Concurrent;
using System.Diagnostics;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Customer;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.Modules.Order.Domain;
using ShowRoom.Modules.Order.Persistence;
using ShowRoom.SharedKernel.Emails;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;
using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

/// <summary>
/// Observability: a single <c>GET .../with-orders</c> produces ONE correlated trace that includes the
/// HTTP server span and the Wolverine messaging span for the Order query, sharing the same trace id.
/// (In the current single-process topology Wolverine executes the handler in-process, so the messaging
/// span is <c>Internal</c>; once the Order module runs as its own service the same trace gains distinct
/// producer/consumer spans across the broker — see the topology note in Program.cs.)
/// </summary>
[Collection("CustomerWithOrders")]
public class MessagingTraceTests(CustomerWithOrdersBusinessWebFactory factory)
{
    private static readonly string CustomersRoute = $"/api/v1/{CustomerConventions.RouteSegment}";

    [Fact]
    public async Task GetCustomerWithOrders_message_call_is_correlated_in_one_trace()
    {
        // Arrange
        var customerPublicId = await SeedAsync();

        var captured = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = captured.Enqueue,
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        var response = await factory.CreateClient().GetAsync($"{CustomersRoute}/{customerPublicId}/with-orders");
        response.EnsureSuccessStatusCode();

        // Assert — the HTTP request and the Wolverine messaging span for the Order query belong to ONE
        // trace. Grouping by trace id is robust to any concurrent requests the process-global listener
        // may also have captured; poll since the messaging span can stop slightly after the response.
        static bool IsCorrelatedTrace(IGrouping<ActivityTraceId, Activity> trace) =>
            trace.Any(a => a.Source.Name == "Microsoft.AspNetCore"
                && a.Kind == ActivityKind.Server
                && a.DisplayName.Contains("with-orders", StringComparison.Ordinal))
            && trace.Any(a => a.Source.Name == "Wolverine");

        IGrouping<ActivityTraceId, Activity>? correlatedTrace = null;
        for (var attempt = 0; attempt < 20 && correlatedTrace is null; attempt++)
        {
            await Task.Delay(250);
            correlatedTrace = captured.ToArray().GroupBy(a => a.TraceId).FirstOrDefault(IsCorrelatedTrace);
        }

        correlatedTrace.Should().NotBeNull(
            "the HTTP GET and the Wolverine messaging span for the Order query must share one trace id, " +
            "so the cross-module call is observable as part of the request's distributed trace");
    }

    private async Task<string> SeedAsync()
    {
        using var scope = factory.Services.CreateScope();
        var customers = scope.ServiceProvider.GetRequiredService<CustomersContext>();
        var orders = scope.ServiceProvider.GetRequiredService<OrdersContext>();

        var customer = CustomerAggregate.Create(
            "Grace", "Hopper", Email.Create($"trace.{Guid.NewGuid():N}@example.com").Value, phone: null, DateTimeOffset.UtcNow);
        customers.Customers.Add(customer);
        await customers.SaveChangesAsync();

        var order = OrderAggregate.Create(
            customer.PublicId,
            "EUR",
            [new OrderLineDraft(PublicIdFactory.ForProduct().Value, "Surf", 1, 10m)],
            DateTimeOffset.UtcNow).Value;
        orders.Orders.Add(order);
        await orders.SaveChangesAsync();

        return customer.PublicId.Value;
    }
}
