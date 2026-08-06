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
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Features.CreateCustomer;

internal sealed class CreateCustomerCommandHandler(
    CustomersContext context,
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
            return Result<PublicId>.Fail(validation.ToErrors());
        }

        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid email");
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
                return Result<PublicId>.Fail(phoneResult.Errors);
            }

            phone = phoneResult.Value;
        }

        var emailAlreadyExists = await context.Customers
            .AnyAsync(customer => customer.Email == email, cancellationToken);

        if (emailAlreadyExists)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Email already exists");
            logger.LogWarning("A customer with the requested email already exists");
            return Result<PublicId>.Fail(CustomerErrors.EmailAlreadyExists);
        }

        var customer = CustomerAggregate.Create(
            command.FirstName,
            command.LastName,
            email,
            phone,
            dateTimeProvider.UtcNow);

        context.Customers.Add(customer);
        await context.SaveChangesAsync(cancellationToken);

        var publicId = CreateCustomerAssembler.From(customer);
        activity?.SetTag("customer.public_id", publicId.Value);
        logger.LogInformation("Customer created with public id {PublicId}", publicId.Value);

        return Result<PublicId>.Success(publicId);
    }
}
