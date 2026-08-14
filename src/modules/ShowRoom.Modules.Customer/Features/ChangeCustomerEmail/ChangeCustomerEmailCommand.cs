namespace ShowRoom.Modules.Customer.Features.ChangeCustomerEmail;

/// <summary>Command: change the email address of the customer identified by <paramref name="PublicId"/>.</summary>
public sealed record ChangeCustomerEmailCommand(string PublicId, string NewEmail);
