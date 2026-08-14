namespace ShowRoom.Modules.Customer.Features.UpdateCustomerProfileCoarse;

/// <summary>Request body for <c>PUT /customers/{publicId}/profile</c> (the id comes from the route).</summary>
public sealed record UpdateCustomerProfileCoarseRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone);
