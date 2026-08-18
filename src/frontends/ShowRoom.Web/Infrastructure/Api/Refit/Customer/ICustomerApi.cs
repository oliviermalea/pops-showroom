using Refit;
using ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models;
using ShowRoom.Web.Infrastructure.Api.Refit.Models;

namespace ShowRoom.Web.Infrastructure.Api.Refit.Customer;

/// <summary>
/// Typed client over the Customer bounded context's HTTP surface (ShowRoom.Customer.Api).
/// Hand-written: the consumed surface is deliberately narrow. It can be swapped for a Refitter-generated
/// interface without touching the callers (see <c>openapi/customer.refitter</c>).
/// </summary>
/// <remarks>
/// Methods return <see cref="ApiResponse{T}"/> so a business outcome carried by a status code (404 on an
/// unknown customer, 400 on a malformed public id) is handled as data by the facade rather than as an
/// exception.
/// </remarks>
[Headers("Accept: application/json")]
public interface ICustomerApi
{
    /// <summary>Lists customers (paginated), optionally filtered by a name search.</summary>
    [Get("/api/v{version}/customers")]
    Task<ApiResponse<PagedResponse<CustomerSummaryResponse>>> GetCustomersAsync(
        int version,
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a customer by its public id.</summary>
    [Get("/api/v{version}/customers/{publicId}")]
    Task<ApiResponse<CustomerDetailResponse>> GetCustomerByPublicIdAsync(
        int version,
        string publicId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a customer with their order history (aggregated from the Order service over AMQP).</summary>
    [Get("/api/v{version}/customers/{publicId}/with-orders")]
    Task<ApiResponse<CustomerWithOrdersResponse>> GetCustomerWithOrdersAsync(
        int version,
        string publicId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a customer; the 201 body is the created public id.</summary>
    [Post("/api/v{version}/customers")]
    Task<ApiResponse<string>> CreateCustomerAsync(
        int version,
        [Body] CreateCustomerRequest request,
        CancellationToken cancellationToken = default);
}
