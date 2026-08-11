using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Features.GetCustomerByPublicId;

/// <summary>
/// Maps the <see cref="CustomerAggregate"/> domain aggregate to its HTTP <see cref="CustomerResponse"/>.
/// </summary>
public static class GetCustomerByPublicIdAssembler
{
    public static CustomerResponse From(CustomerAggregate customer)
        => new(
            PublicId: customer.PublicId,
            FirstName: customer.FirstName,
            LastName: customer.LastName,
            DisplayName: customer.DisplayName,
            Email: customer.Email.Value,
            Phone: customer.Phone?.Value,
            Status: customer.Status.Value,
            RegisteredOn: customer.CreatedAt);
}
