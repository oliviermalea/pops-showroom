using ShowRoom.Web.Features.Customer.CreateCustomer;

namespace ShowRoom.Web.Features.Customer;

/// <summary>
/// Single entry point of the Customer module for the UI. Components never touch the typed API client:
/// they orchestrate this facade, which owns the call, the status handling and the mapping.
/// </summary>
public interface ICustomerFacade
{
    /// <summary>Loads one page of customers, optionally filtered by a free-text name search.</summary>
    Task<CustomerListResult> GetCustomersAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>Loads a customer detail by its public id.</summary>
    Task<CustomerLookupResult> GetCustomerAsync(string? publicId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a customer with their order history. The history comes from another service over the
    /// bus, so it may legitimately be unavailable while the customer is returned.
    /// </summary>
    Task<CustomerOrdersResult> GetCustomerOrdersAsync(string? publicId, CancellationToken cancellationToken = default);

    /// <summary>Creates a customer from the screen's form model.</summary>
    Task<CustomerCreationResult> CreateCustomerAsync(CreateCustomerForm form, CancellationToken cancellationToken = default);
}
