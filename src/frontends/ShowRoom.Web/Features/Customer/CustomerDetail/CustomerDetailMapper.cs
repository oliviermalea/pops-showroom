using ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models;

namespace ShowRoom.Web.Features.Customer.CustomerDetail;

/// <summary>
/// Maps the API contract to the screen's view model. Formatting lives here — never in the component —
/// so it stays explicit and testable; the rules themselves are shared through <see cref="CustomerFormat"/>.
/// </summary>
public static class CustomerDetailMapper
{
    /// <summary>Placeholder shown for an optional value the customer did not provide.</summary>
    public const string NotProvided = CustomerFormat.NotProvided;

    public static CustomerDetailView FromApi(CustomerDetailResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new CustomerDetailView(
            PublicId: response.PublicId,
            DisplayName: CustomerFormat.DisplayName(response.DisplayName, response.FirstName, response.LastName),
            FirstName: response.FirstName,
            LastName: response.LastName,
            Email: response.Email,
            Phone: CustomerFormat.OrPlaceholder(response.Phone),
            Status: response.Status,
            IsActive: CustomerFormat.IsActive(response.Status),
            RegisteredOn: CustomerFormat.Date(response.RegisteredOn));
    }
}
