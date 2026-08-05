using FluentValidation;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Application.Validations;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Observability.Logging;
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
        using var scope = logger.BeginModuleScope(OrderConventions.ModuleName, FeatureName);
        logger.LogInformation("Processing order creation");

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            logger.LogWarning("Validation failed for create order request");
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
            logger.LogWarning("Order creation rejected by domain rules");
            return Result<PublicId>.Fail(orderResult.Errors);
        }

        var order = orderResult.Value;

        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        var publicId = CreateOrderAssembler.From(order);
        logger.LogInformation("Order created with public id {PublicId}", publicId.Value);

        return Result<PublicId>.Success(publicId);
    }
}
