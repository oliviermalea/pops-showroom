namespace ShowRoom.Web.Features.Customer.CustomerDetail;

/// <summary>
/// View model of the customer detail screen: display-ready values only (already formatted), so the
/// component holds no transformation logic.
/// </summary>
public sealed record CustomerDetailView(
    string PublicId,
    string DisplayName,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Status,
    bool IsActive,
    string RegisteredOn);
