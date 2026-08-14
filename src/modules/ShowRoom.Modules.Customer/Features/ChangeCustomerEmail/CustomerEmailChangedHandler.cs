using Microsoft.Extensions.Logging;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.Modules.Customer.Domain;

namespace ShowRoom.Modules.Customer.Features.ChangeCustomerEmail;

/// <summary>
/// Reacts to <see cref="CustomerEmailChanged"/> in-process (dispatched by the EF interceptor after commit).
/// Demonstration handler: it only logs. A real reaction (e.g. notify the customer, publish an integration
/// event) belongs here too — but it must not write back to the Customer DbContext.
/// </summary>
internal sealed class CustomerEmailChangedHandler(ILogger<CustomerEmailChangedHandler> logger)
    : IDomainEventHandler<CustomerEmailChanged>
{
    public Task Handle(CustomerEmailChanged domainEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Customer {PublicId} changed email from {PreviousEmail} to {NewEmail}",
            domainEvent.PublicId.Value,
            domainEvent.PreviousEmail,
            domainEvent.NewEmail);

        return Task.CompletedTask;
    }
}
