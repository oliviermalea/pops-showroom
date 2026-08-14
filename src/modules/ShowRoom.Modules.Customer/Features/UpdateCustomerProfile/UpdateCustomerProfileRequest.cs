namespace ShowRoom.Modules.Customer.Features.UpdateCustomerProfile;

/// <summary>Request body for <c>PUT /customers/{publicId}</c> (the id comes from the route).</summary>
public sealed record UpdateCustomerProfileRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone);
