using ShowRoom.Web.Client.Features.Catalog;

namespace ShowRoom.Web.Tests.Doubles;

/// <summary>
/// Hand-written double of the catalogue facade — no mocking framework, per the repository convention.
/// Each screen scenario is expressed by handing the stub the outcome the facade would produce.
/// </summary>
public sealed class StubCatalogFacade : ICatalogFacade
{
    private readonly TaskCompletionSource? gate;

    public StubCatalogFacade(TaskCompletionSource? gate = null) => this.gate = gate;

    public ProductListResult ListResult { get; set; } = ProductListResult.Unavailable();

    public ProductLookupResult LookupResult { get; set; } = ProductLookupResult.Unavailable();

    public (int Page, int PageSize)? LastListQuery { get; private set; }

    public string? LastPublicId { get; private set; }

    public int CallCount { get; private set; }

    public async Task<ProductListResult> GetProductsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastListQuery = (page, pageSize);
        await WaitForGateAsync();
        return ListResult;
    }

    public async Task<ProductLookupResult> GetProductAsync(
        string? publicId,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastPublicId = publicId;
        await WaitForGateAsync();
        return LookupResult;
    }

    /// <summary>Suspends the answer until the test releases the gate — used to observe loading states.</summary>
    private Task WaitForGateAsync() => gate?.Task ?? Task.CompletedTask;
}
