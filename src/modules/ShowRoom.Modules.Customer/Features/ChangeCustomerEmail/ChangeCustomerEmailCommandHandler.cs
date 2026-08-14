using System.Diagnostics;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Application.Validations;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Observability.Logging;
using ShowRoom.BuildingBlocks.Observability.Tracing;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.BuildingBlocks.Time;
using ShowRoom.Modules.Customer.Domain;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.SharedKernel.Emails;

namespace ShowRoom.Modules.Customer.Features.ChangeCustomerEmail;

internal sealed class ChangeCustomerEmailCommandHandler(
    CustomersContext context,
    IDateTimeProvider dateTimeProvider,
    IValidator<ChangeCustomerEmailCommand> validator,
    ILogger<ChangeCustomerEmailCommandHandler> logger)
    : ICommandHandler<ChangeCustomerEmailCommand, Result>
{
    private const string FeatureName = "ChangeCustomerEmail";

    public async Task<Result> Handle(ChangeCustomerEmailCommand command, CancellationToken cancellationToken)
    {
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(CustomerModule.ModuleName, FeatureName, requestId);
        using var activity = CustomerModule.ActivitySource.StartActivity("customer.change_customer_email");

        activity?
            .SetCommonTags(CustomerModule.ModuleName, FeatureName, requestId)
            .SetTag("customer.public_id", command.PublicId);

        logger.LogInformation("Processing customer email change");

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
            logger.LogWarning("Validation failed for change customer email request");
            return Result.Fail(validation.ToErrors());
        }

        var publicId = PublicId.Parse(command.PublicId);
        var newEmail = Email.Create(command.NewEmail).Value; // format already validated above

        var customer = await context.Customers
            .SingleOrDefaultAsync(item => item.PublicId == publicId, cancellationToken);

        if (customer is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Customer not found");
            logger.LogWarning("Customer {PublicId} not found", publicId.Value);
            return CustomerErrors.NotFound(publicId);
        }

        // Cross-aggregate uniqueness: the address must not already belong to a DIFFERENT customer.
        var emailTakenByAnother = await context.Customers
            .AnyAsync(item => item.Email == newEmail && item.Id != customer.Id, cancellationToken);

        if (emailTakenByAnother)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Email already exists");
            logger.LogWarning("Cannot change email: another customer already uses it");
            return CustomerErrors.EmailAlreadyExists;
        }

        var changeResult = customer.ChangeEmail(newEmail, dateTimeProvider.UtcNow);
        if (changeResult.IsFailure)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Domain rejected the email change");
            return changeResult;
        }

        // The aggregate has raised CustomerEmailChanged. Dispatching domain events at SaveChanges is
        // intentionally NOT wired here yet — pending the chosen outbox/dispatch strategy.
        await context.SaveChangesAsync(cancellationToken);

        activity?.SetTag("customer.email_changed", true);
        logger.LogInformation("Customer {PublicId} email changed", publicId.Value);

        return Result.Success();
    }
}
