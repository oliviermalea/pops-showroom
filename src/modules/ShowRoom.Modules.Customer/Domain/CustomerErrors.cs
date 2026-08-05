using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Customer.Domain;

/// <summary>
/// Well-known errors for the Customer domain, exposed as stable <see cref="Error"/> instances.
/// </summary>
public static class CustomerErrors
{
    public static Error NotFound(PublicId publicId)
        => Error.NotFound("Customer.NotFound", $"No customer was found with public id '{publicId}'.");

    public static readonly Error EmailAlreadyExists = Error.Conflict(
        "Customer.EmailAlreadyExists",
        "A customer with this email already exists.");
}
