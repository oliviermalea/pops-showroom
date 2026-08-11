using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Features.GetCustomers;

/// <summary>Maps the <see cref="CustomerAggregate"/> domain aggregate to its compact list summary.</summary>
public static class GetCustomersAssembler
{
    public static CustomerSummaryResponse ToSummary(CustomerAggregate customer)
        => new(
            PublicId: customer.PublicId,
            FirstName: customer.FirstName,
            LastName: customer.LastName,
            DisplayName: customer.DisplayName,
            Email: customer.Email.Value,
            Status: customer.Status.Value,
            RegisteredOn: customer.CreatedAt);
}
