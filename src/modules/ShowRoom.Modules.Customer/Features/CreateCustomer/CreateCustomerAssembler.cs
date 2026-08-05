using ShowRoom.BuildingBlocks.Domain.PublicIds;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Features.CreateCustomer;

/// <summary>Maps a freshly created <see cref="CustomerAggregate"/> to its public identifier.</summary>
internal static class CreateCustomerAssembler
{
    internal static PublicId From(CustomerAggregate customer) => customer.PublicId;
}
