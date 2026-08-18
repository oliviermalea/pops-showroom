namespace ShowRoom.Web.Features.Customer.CreateCustomer;

/// <summary>
/// Form model of the customer creation screen. Plain POCO with settable properties (two-way binding);
/// the rules live in <see cref="CreateCustomerFormValidator"/>, never in DataAnnotations.
/// </summary>
public sealed class CreateCustomerForm
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }
}
