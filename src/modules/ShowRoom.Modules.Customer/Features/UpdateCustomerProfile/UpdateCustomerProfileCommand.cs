namespace ShowRoom.Modules.Customer.Features.UpdateCustomerProfile;

/// <summary>Command: update the editable profile of the customer identified by <paramref name="PublicId"/>.</summary>
public sealed record UpdateCustomerProfileCommand(
    string PublicId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone);
