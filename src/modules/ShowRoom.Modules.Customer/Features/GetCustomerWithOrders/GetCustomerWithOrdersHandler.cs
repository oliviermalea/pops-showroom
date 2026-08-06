using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.Modules.Customer.Domain;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.Modules.Order.Contracts.Messaging;

namespace ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;

/// <summary>
/// Loads the customer from the Customer module's own database, then fetches the order history from the
/// Order module over the message bus (AMQP request/reply). If the Order module cannot be reached in
/// time, the customer is still returned with <c>OrdersAvailable = false</c> (reactive graceful
/// degradation) rather than failing the whole request.
/// </summary>
public sealed class GetCustomerWithOrdersHandler(
    CustomersContext context,
    IOrderQueryGateway orderQueryGateway,
    ILogger<GetCustomerWithOrdersHandler> logger)
    : IQueryHandler<GetCustomerWithOrdersQuery, Result<CustomerWithOrdersResponse>>
{
    private const string FeatureName = "GetCustomerWithOrders";

    public async Task<Result<CustomerWithOrdersResponse>> HandleAsync(
        GetCustomerWithOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        // Correlate the log scope and span with the ambient trace so this call and the downstream
        // RabbitMQ request/reply (propagated by Wolverine) share the same request/trace id.
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(CustomerModule.ModuleName, FeatureName, requestId);
        using var activity = CustomerModule.ActivitySource.StartActivity("customer.get_customer_with_orders");

        var publicId = query.PublicId;
        activity?
            .SetCommonTags(CustomerModule.ModuleName, FeatureName, requestId)
            .SetTag("customer.public_id", publicId.Value);

        var customer = await context.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.PublicId == publicId, cancellationToken);

        if (customer is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Customer not found");
            logger.LogWarning("Customer {PublicId} not found", publicId.Value);
            return CustomerErrors.NotFound(publicId);
        }

        IReadOnlyCollection<CustomerOrderSummary> orders = [];
        var ordersAvailable = false;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogInformation("Requesting orders for customer {PublicId} over the message bus", publicId.Value);
            var response = await orderQueryGateway.GetOrdersForCustomerAsync(publicId.Value, cancellationToken);
            orders = response.Orders;
            ordersAvailable = true;
            logger.LogInformation("Received {Count} orders for customer {PublicId}", orders.Count, publicId.Value);
        }
        catch (Exception exception)
        {
            // Reactive degradation: the Order module / broker is unreachable or timed out. Return the
            // customer without their orders and flag it, rather than failing the whole request.
            activity?.AddException(exception);
            activity?.SetStatus(ActivityStatusCode.Error, "Order module unavailable");
            logger.LogWarning(
                exception,
                "Order module unavailable for customer {PublicId}; returning degraded result",
                publicId.Value);
        }
        finally
        {
            stopwatch.Stop();
        }

        activity?
            .SetTag("customer.orders.available", ordersAvailable)
            .SetTag("customer.orders.count", orders.Count)
            .SetTag("customer.orders.roundtrip_ms", stopwatch.ElapsedMilliseconds);

        return Result<CustomerWithOrdersResponse>.Success(
            GetCustomerWithOrdersAssembler.From(customer, orders, ordersAvailable));
    }
}
