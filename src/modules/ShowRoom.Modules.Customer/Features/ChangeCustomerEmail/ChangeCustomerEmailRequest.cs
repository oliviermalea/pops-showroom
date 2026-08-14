namespace ShowRoom.Modules.Customer.Features.ChangeCustomerEmail;

/// <summary>Request body for <c>PATCH /customers/{publicId}/email</c> (the id comes from the route).</summary>
public sealed record ChangeCustomerEmailRequest(string NewEmail);
