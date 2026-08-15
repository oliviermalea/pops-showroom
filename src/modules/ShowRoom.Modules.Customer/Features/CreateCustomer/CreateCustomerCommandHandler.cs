using System.Diagnostics;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Application.Validations;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Messaging;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.BuildingBlocks.Time;
using ShowRoom.Modules.Customer.Contracts.Messaging;
using ShowRoom.Modules.Customer.Domain;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.SharedKernel.Emails;
using ShowRoom.SharedKernel.PhoneNumbers;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Features.CreateCustomer;

/// <summary>
/// Creates a customer and publishes <see cref="CustomerRegisteredIntegrationEvent"/> through Wolverine's
/// transactional outbox. Uses <see cref="IDbContextOutbox{TContext}"/> over the SINGLE
/// <see cref="CustomersContext"/> so the outgoing envelope is stored in the SAME database transaction as
/// the customer insert (atomic) via
/// <see cref="IDbContextOutbox{TContext}.SaveChangesAndFlushMessagesAsync(System.Threading.CancellationToken)"/>;
/// a durable sending agent then delivers it to RabbitMQ with retries (guaranteed at-least-once). The
/// Wolverine tables live in the dedicated <c>wolverine</c> schema of the same database.
/// </summary>
internal sealed class CreateCustomerCommandHandler(
    IDbContextOutbox<CustomersContext> outbox,
    IDateTimeProvider dateTimeProvider,
    IValidator<CreateCustomerCommand> validator,
    ILogger<CreateCustomerCommandHandler> logger)
    : ICommandHandler<CreateCustomerCommand, Result<PublicId>>
{
    private const string FeatureName = "CreateCustomer";

    public async Task<Result<PublicId>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(CustomerModule.ModuleName, FeatureName, requestId);
        using var activity = CustomerModule.ActivitySource.StartActivity("customer.create_customer");

        activity?
            .SetCommonTags(CustomerModule.ModuleName, FeatureName, requestId)
            .SetTag("enduser.email", command.Email)
            .SetTag("customer.has_phone", !string.IsNullOrWhiteSpace(command.Phone));

        logger.LogInformation("Processing customer creation");

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
            logger.LogWarning("Validation failed for create customer request");
            CreateCustomerMetrics.RecordRejected("validation");
            return Result<PublicId>.Fail(validation.ToErrors());
        }

        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid email");
            CreateCustomerMetrics.RecordRejected("validation");
            return Result<PublicId>.Fail(emailResult.Errors);
        }

        var email = emailResult.Value;

        PhoneNumber? phone = null;
        if (!string.IsNullOrWhiteSpace(command.Phone))
        {
            var phoneResult = PhoneNumber.Create(command.Phone);
            if (phoneResult.IsFailure)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Invalid phone number");
                CreateCustomerMetrics.RecordRejected("validation");
                return Result<PublicId>.Fail(phoneResult.Errors);
            }

            phone = phoneResult.Value;
        }

        var context = outbox.DbContext;

        var emailAlreadyExists = await context.Customers
            .AnyAsync(customer => customer.Email == email, cancellationToken);

        if (emailAlreadyExists)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Email already exists");
            logger.LogWarning("A customer with the requested email already exists");
            CreateCustomerMetrics.RecordRejected("conflict");
            return Result<PublicId>.Fail(CustomerErrors.EmailAlreadyExists);
        }

        var customer = CustomerAggregate.Create(
            command.FirstName,
            command.LastName,
            email,
            phone,
            dateTimeProvider.UtcNow);

        context.Customers.Add(customer);

        var publicId = CreateCustomerAssembler.From(customer);

        // Publish the IntegrationEvent through the transactional outbox: the envelope is persisted in the
        // SAME transaction as the customer insert (atomic all-or-nothing), then a durable agent delivers it
        // to RabbitMQ with retries. This is the sanctioned DbContext write for the outbox (see backend §3.3).
        try
        {
            await outbox.PublishAsync(
                new CustomerRegisteredIntegrationEvent(
                    publicId.Value,
                    customer.FirstName,
                    customer.LastName,
                    customer.Email.Value,
                    customer.CreatedAt),
                BuildOutboxDeliveryOptions(requestId));

            await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            // The atomic write failed (store/DB unavailable) — nothing was committed. Mark the span and
            // log so the failure is diagnosable, then let it surface as a 500 via the exception handler.
            activity?.SetStatus(ActivityStatusCode.Error, "Outbox publish failed");
            activity?.AddException(exception);
            logger.LogError(exception, "Failed to persist and publish CustomerRegistered via the outbox");
            throw;
        }

        activity?
            .SetTag("customer.public_id", publicId.Value)
            .SetTag("messaging.outbox.published", true);
        CreateCustomerMetrics.RecordRegistered();
        logger.LogInformation(
            "Customer created with public id {PublicId}; CustomerRegistered integration event enqueued to the outbox",
            publicId.Value);

        return Result<PublicId>.Success(publicId);
    }

    private static DeliveryOptions BuildOutboxDeliveryOptions(string requestId)
        => new DeliveryOptions()
            .WithHeader(MessageHeaders.ModuleName, CustomerModule.ModuleName)
            .WithHeader(MessageHeaders.FeatureName, FeatureName)
            .WithHeader(MessageHeaders.MessageType, nameof(CustomerRegisteredIntegrationEvent))
            .WithHeader(MessageHeaders.EventType, nameof(CustomerRegisteredIntegrationEvent))
            .WithHeader(MessageHeaders.MessageId, Guid.NewGuid().ToString("N"))
            .WithHeader(MessageHeaders.CorrelationId, requestId)
            .WithHeader(MessageHeaders.TraceId, requestId);
}
