using ShowRoom.Web.Features.Customer;
using ShowRoom.Web.Features.Customer.CreateCustomer;
using ShowRoom.Web.Features.Customer.CustomerDetail;
using ShowRoom.Web.Features.Customer.CustomerList;
using ShowRoom.Web.Features.Customer.CustomerOrders;

namespace ShowRoom.Web.Tests.Doubles;

/// <summary>
/// Hand-written double of the Customer facade — no mocking framework, per the repository convention.
/// Each screen scenario is expressed by handing the stub the outcome the facade would produce, so the
/// test reads as « given the service answers X, the screen shows Y ».
/// </summary>
public sealed class StubCustomerFacade : ICustomerFacade
{
    private readonly TaskCompletionSource? gate;

    public StubCustomerFacade(TaskCompletionSource? gate = null) => this.gate = gate;

    public CustomerListResult ListResult { get; set; } = CustomerListResult.Unavailable();

    public CustomerLookupResult LookupResult { get; set; } = CustomerLookupResult.Unavailable();

    public CustomerOrdersResult OrdersResult { get; set; } = CustomerOrdersResult.Unavailable();

    public CustomerCreationResult CreationResult { get; set; } = CustomerCreationResult.Unavailable();

    public (int Page, int PageSize, string? Search)? LastListQuery { get; private set; }

    public string? LastPublicId { get; private set; }

    public CreateCustomerForm? LastForm { get; private set; }

    public int CallCount { get; private set; }

    public async Task<CustomerListResult> GetCustomersAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastListQuery = (page, pageSize, search);
        await WaitForGateAsync();
        return ListResult;
    }

    public async Task<CustomerLookupResult> GetCustomerAsync(
        string? publicId,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastPublicId = publicId;
        await WaitForGateAsync();
        return LookupResult;
    }

    public async Task<CustomerOrdersResult> GetCustomerOrdersAsync(
        string? publicId,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastPublicId = publicId;
        await WaitForGateAsync();
        return OrdersResult;
    }

    public async Task<CustomerCreationResult> CreateCustomerAsync(
        CreateCustomerForm form,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastForm = form;
        await WaitForGateAsync();
        return CreationResult;
    }

    /// <summary>Suspends the answer until the test releases the gate — used to observe loading states.</summary>
    private Task WaitForGateAsync() => gate?.Task ?? Task.CompletedTask;
}
