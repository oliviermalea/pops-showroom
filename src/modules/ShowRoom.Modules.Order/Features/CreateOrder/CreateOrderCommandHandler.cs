using System.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Application.Validations;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.BuildingBlocks.Time;
using ShowRoom.Modules.Order.Persistence;
using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Features.CreateOrder;

internal sealed class CreateOrderCommandHandler(
    OrdersContext context,
    IDateTimeProvider dateTimeProvider,
    IValidator<CreateOrderCommand> validator,
    ILogger<CreateOrderCommandHandler> logger)
    : ICommandHandler<CreateOrderCommand, Result<PublicId>>
{
    private const string FeatureName = "CreateOrder";

    public async Task<Result<PublicId>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(OrderModule.ModuleName, FeatureName, requestId);
        using var activity = OrderModule.ActivitySource.StartActivity("order.create_order");

        activity?
            .SetCommonTags(OrderModule.ModuleName, FeatureName, requestId)
            .SetTag("order.customer.public_id", command.CustomerPublicId)
            .SetTag("order.line_count", command.Lines.Count);

        logger.LogInformation("Processing order creation");

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
            logger.LogWarning("Validation failed for create order request");
            CreateOrderMetrics.RecordRejected("validation");
            return Result<PublicId>.Fail(validation.ToErrors());
        }

        // Cross-module boundary: the customer is referenced by public id only. We deliberately do NOT
        // reach into the Customer module's database here — existence is a separate integration concern.
        var customerPublicId = PublicId.Parse(command.CustomerPublicId);
        var drafts = CreateOrderAssembler.ToDrafts(command);

        var orderResult = OrderAggregate.Create(
            customerPublicId,
            command.Currency,
            drafts,
            dateTimeProvider.UtcNow);

        if (orderResult.IsFailure)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Domain rules rejected the order");
            logger.LogWarning("Order creation rejected by domain rules");
            CreateOrderMetrics.RecordRejected("domain");
            return Result<PublicId>.Fail(orderResult.Errors);
        }

        var order = orderResult.Value;

        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        var publicId = CreateOrderAssembler.From(order);
        activity?
            .SetTag("order.public_id", publicId.Value)
            .SetTag("order.total_amount", order.TotalAmount)
            .SetTag("order.currency", order.Currency.Code);
        CreateOrderMetrics.RecordCreated(order.TotalAmount, order.Currency.Code, order.Lines.Count);
        logger.LogInformation("Order created with public id {PublicId}", publicId.Value);

        return Result<PublicId>.Success(publicId);
    }
}
