namespace ShowRoom.Modules.Customer.Features.CreateCustomer;

/// <summary>Command: create a new customer. Payload of the create endpoint.</summary>
public sealed record CreateCustomerCommand
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
}
