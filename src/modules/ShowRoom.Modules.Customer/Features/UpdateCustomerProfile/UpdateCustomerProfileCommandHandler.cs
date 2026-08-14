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
using ShowRoom.SharedKernel.PhoneNumbers;

namespace ShowRoom.Modules.Customer.Features.UpdateCustomerProfile;

internal sealed class UpdateCustomerProfileCommandHandler(
    CustomersContext context,
    IDateTimeProvider dateTimeProvider,
    IValidator<UpdateCustomerProfileCommand> validator,
    ILogger<UpdateCustomerProfileCommandHandler> logger)
    : ICommandHandler<UpdateCustomerProfileCommand, Result>
{
    private const string FeatureName = "UpdateCustomerProfile";

    public async Task<Result> Handle(UpdateCustomerProfileCommand command, CancellationToken cancellationToken)
    {
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(CustomerModule.ModuleName, FeatureName, requestId);
        using var activity = CustomerModule.ActivitySource.StartActivity("customer.update_customer_profile");

        activity?
            .SetCommonTags(CustomerModule.ModuleName, FeatureName, requestId)
            .SetTag("customer.public_id", command.PublicId);

        logger.LogInformation("Processing customer profile update");

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
            logger.LogWarning("Validation failed for update customer profile request");
            return Result.Fail(validation.ToErrors());
        }

        var publicId = PublicId.Parse(command.PublicId);
        var email = Email.Create(command.Email).Value; // format already validated above

        PhoneNumber? phone = null;
        if (!string.IsNullOrWhiteSpace(command.Phone))
        {
            var phoneResult = PhoneNumber.Create(command.Phone);
            if (phoneResult.IsFailure)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Invalid phone number");
                return Result.Fail(phoneResult.Errors);
            }

            phone = phoneResult.Value;
        }

        var customer = await context.Customers
            .SingleOrDefaultAsync(item => item.PublicId == publicId, cancellationToken);

        if (customer is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Customer not found");
            logger.LogWarning("Customer {PublicId} not found", publicId.Value);
            return CustomerErrors.NotFound(publicId);
        }

        // Cross-aggregate uniqueness: the email must not already belong to a DIFFERENT customer.
        var emailTakenByAnother = await context.Customers
            .AnyAsync(item => item.Email == email && item.Id != customer.Id, cancellationToken);

        if (emailTakenByAnother)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Email already exists");
            logger.LogWarning("Cannot update profile: another customer already uses the email");
            return CustomerErrors.EmailAlreadyExists;
        }

        var now = dateTimeProvider.UtcNow;

        // Task-based orchestration: each intention is idempotent and raises its own event; they share one
        // 'now' and commit atomically in a single SaveChanges. If any step fails, nothing is persisted and
        // no event is dispatched (the interceptor only runs after commit).
        var rename = customer.Rename(command.FirstName, command.LastName, now);
        if (rename.IsFailure)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Domain rejected the rename");
            return rename;
        }

        var emailChange = customer.ChangeEmail(email, now);
        if (emailChange.IsFailure)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Domain rejected the email change");
            return emailChange;
        }

        customer.ChangePhone(phone, now);

        await context.SaveChangesAsync(cancellationToken);

        activity?.SetTag("customer.profile_updated", true);
        logger.LogInformation("Customer {PublicId} profile updated", publicId.Value);

        return Result.Success();
    }
}
