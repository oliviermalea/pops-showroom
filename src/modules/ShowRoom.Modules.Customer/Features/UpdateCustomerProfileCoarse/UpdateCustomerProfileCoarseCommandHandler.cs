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

namespace ShowRoom.Modules.Customer.Features.UpdateCustomerProfileCoarse;

/// <summary>
/// "Style 2" handler. Same boilerplate as the task-based one (load, validate, uniqueness), but the whole
/// mutation collapses into a SINGLE aggregate call — <c>customer.UpdateProfile(...)</c> — which raises one
/// <c>CustomerProfileUpdated</c>. Compare with <c>UpdateCustomerProfileCommandHandler</c> (three calls,
/// three fine-grained events).
/// </summary>
internal sealed class UpdateCustomerProfileCoarseCommandHandler(
    CustomersContext context,
    IDateTimeProvider dateTimeProvider,
    IValidator<UpdateCustomerProfileCoarseCommand> validator,
    ILogger<UpdateCustomerProfileCoarseCommandHandler> logger)
    : ICommandHandler<UpdateCustomerProfileCoarseCommand, Result>
{
    private const string FeatureName = "UpdateCustomerProfileCoarse";

    public async Task<Result> Handle(UpdateCustomerProfileCoarseCommand command, CancellationToken cancellationToken)
    {
        var requestId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using var scope = logger.BeginModuleScope(CustomerModule.ModuleName, FeatureName, requestId);
        using var activity = CustomerModule.ActivitySource.StartActivity("customer.update_customer_profile_coarse");

        activity?
            .SetCommonTags(CustomerModule.ModuleName, FeatureName, requestId)
            .SetTag("customer.public_id", command.PublicId);

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
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
            return CustomerErrors.NotFound(publicId);
        }

        var emailTakenByAnother = await context.Customers
            .AnyAsync(item => item.Email == email && item.Id != customer.Id, cancellationToken);

        if (emailTakenByAnother)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Email already exists");
            return CustomerErrors.EmailAlreadyExists;
        }

        // Style 2: one coarse call, one CustomerProfileUpdated event.
        var result = customer.UpdateProfile(command.FirstName, command.LastName, email, phone, dateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Domain rejected the profile update");
            return result;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Customer {PublicId} profile updated (coarse)", publicId.Value);

        return Result.Success();
    }
}
