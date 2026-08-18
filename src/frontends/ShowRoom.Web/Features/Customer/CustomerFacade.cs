using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ShowRoom.Web.Features.Customer.CreateCustomer;
using ShowRoom.Web.Features.Customer.CustomerDetail;
using ShowRoom.Web.Features.Customer.CustomerList;
using ShowRoom.Web.Features.Customer.CustomerOrders;
using ShowRoom.Web.Infrastructure.Api;
using ShowRoom.Web.Infrastructure.Api.Refit.Customer;
using ShowRoom.Web.Infrastructure.PublicIds;

namespace ShowRoom.Web.Features.Customer;

/// <summary>
/// Orchestrates the Customer HTTP surface for the UI: boundary validation, call through the Refit
/// client, translation of the status codes into an explicit <see cref="CustomerLookupOutcome"/>, and
/// mapping to the view model. Every branch is logged so the screen stays diagnosable in production.
/// </summary>
public sealed class CustomerFacade(
    ICustomerApi customerApi,
    IOptions<BackendApiOptions> options,
    ILogger<CustomerFacade> logger) : ICustomerFacade
{
    private const string Feature = "CustomerDetail";
    private const string CreateFeature = "CreateCustomer";
    private const string ListFeature = "CustomerList";
    private const string OrdersFeature = "CustomerOrders";

    /// <summary>Upper bound applied to the requested page size, mirroring a sane server-side limit.</summary>
    internal const int MaxPageSize = 100;

    /// <summary>Page size used when the caller does not provide a valid one.</summary>
    internal const int DefaultPageSize = 20;

    public async Task<CustomerListResult> GetCustomersAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        // Normalised here so a hand-edited query string can never turn into a 400 from the backend.
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize is < 1 or > MaxPageSize ? DefaultPageSize : pageSize;
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        try
        {
            var response = await customerApi.GetCustomersAsync(
                options.Value.ApiVersion,
                normalizedPage,
                normalizedPageSize,
                normalizedSearch,
                cancellationToken);

            if (response.IsSuccessStatusCode && response.Content is not null)
            {
                var view = CustomerListMapper.FromApi(response.Content);

                logger.LogInformation(
                    "[{Feature}] Loaded page {Page} ({Count} of {TotalItems} customers, search: {Search})",
                    ListFeature,
                    view.Page,
                    view.Items.Count,
                    view.TotalItems,
                    normalizedSearch ?? "none");

                return CustomerListResult.Loaded(view);
            }

            logger.LogError(
                response.Error,
                "[{Feature}] Unexpected status {StatusCode} while listing customers",
                ListFeature,
                response.StatusCode);

            return CustomerListResult.Unavailable();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "[{Feature}] Customer service unreachable while listing customers", ListFeature);
            return CustomerListResult.Unavailable();
        }
    }

    public async Task<CustomerLookupResult> GetCustomerAsync(
        string? publicId,
        CancellationToken cancellationToken = default)
    {
        if (!PublicIdFormat.IsValid(publicId))
        {
            logger.LogWarning("[{Feature}] Rejected malformed public id {PublicId}", Feature, publicId);
            return CustomerLookupResult.InvalidPublicId();
        }

        var normalized = publicId!.Trim();

        try
        {
            var response = await customerApi.GetCustomerByPublicIdAsync(
                options.Value.ApiVersion,
                normalized,
                cancellationToken);

            if (response.IsSuccessStatusCode && response.Content is not null)
            {
                logger.LogInformation("[{Feature}] Loaded customer {PublicId}", Feature, normalized);
                return CustomerLookupResult.Found(CustomerDetailMapper.FromApi(response.Content));
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("[{Feature}] No customer found for {PublicId}", Feature, normalized);
                return CustomerLookupResult.NotFound();
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                logger.LogWarning("[{Feature}] Backend rejected public id {PublicId}", Feature, normalized);
                return CustomerLookupResult.InvalidPublicId();
            }

            logger.LogError(
                response.Error,
                "[{Feature}] Unexpected status {StatusCode} while loading customer {PublicId}",
                Feature,
                response.StatusCode,
                normalized);

            return CustomerLookupResult.Unavailable();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Transport-level failure (service down, DNS, timeout after the resilience handler gave up):
            // degrade to an explicit "unavailable" state instead of surfacing an unhandled exception.
            logger.LogError(exception, "[{Feature}] Customer service unreachable for {PublicId}", Feature, normalized);
            return CustomerLookupResult.Unavailable();
        }
    }

    public async Task<CustomerOrdersResult> GetCustomerOrdersAsync(
        string? publicId,
        CancellationToken cancellationToken = default)
    {
        if (!PublicIdFormat.IsValid(publicId))
        {
            logger.LogWarning("[{Feature}] Rejected malformed public id {PublicId}", OrdersFeature, publicId);
            return CustomerOrdersResult.InvalidPublicId();
        }

        var normalized = publicId!.Trim();

        try
        {
            var response = await customerApi.GetCustomerWithOrdersAsync(
                options.Value.ApiVersion,
                normalized,
                cancellationToken);

            if (response.IsSuccessStatusCode && response.Content is not null)
            {
                var view = CustomerOrdersMapper.FromApi(response.Content);

                // A 200 with ordersAvailable = false is a PARTIAL success: the Customer service could
                // not reach the Order service over the bus and degraded on purpose. Log it as such so
                // the degradation stays visible in production, and let the screen say it.
                if (view.OrdersAvailable)
                {
                    logger.LogInformation(
                        "[{Feature}] Loaded {OrderCount} orders for customer {PublicId}",
                        OrdersFeature,
                        view.Orders.Count,
                        normalized);
                }
                else
                {
                    logger.LogWarning(
                        "[{Feature}] Order history unavailable for customer {PublicId} (Order service unreachable)",
                        OrdersFeature,
                        normalized);
                }

                return CustomerOrdersResult.Found(view);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("[{Feature}] No customer found for {PublicId}", OrdersFeature, normalized);
                return CustomerOrdersResult.NotFound();
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                logger.LogWarning("[{Feature}] Backend rejected public id {PublicId}", OrdersFeature, normalized);
                return CustomerOrdersResult.InvalidPublicId();
            }

            logger.LogError(
                response.Error,
                "[{Feature}] Unexpected status {StatusCode} while loading the orders of {PublicId}",
                OrdersFeature,
                response.StatusCode,
                normalized);

            return CustomerOrdersResult.Unavailable();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "[{Feature}] Customer service unreachable for {PublicId}", OrdersFeature, normalized);
            return CustomerOrdersResult.Unavailable();
        }
    }

    public async Task<CustomerCreationResult> CreateCustomerAsync(
        CreateCustomerForm form,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);

        var request = CreateCustomerMapper.ToApi(form);

        try
        {
            var response = await customerApi.CreateCustomerAsync(
                options.Value.ApiVersion,
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var publicId = ReadCreatedPublicId(response.Content);

                if (publicId is not null)
                {
                    logger.LogInformation("[{Feature}] Created customer {PublicId}", CreateFeature, publicId);
                    return CustomerCreationResult.Created(publicId);
                }

                logger.LogError("[{Feature}] Created customer but the response carried no public id", CreateFeature);
                return CustomerCreationResult.Unavailable();
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                logger.LogInformation("[{Feature}] Email already used: {Email}", CreateFeature, request.Email);
                return CustomerCreationResult.EmailAlreadyUsed();
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                logger.LogWarning("[{Feature}] Backend rejected the payload for {Email}", CreateFeature, request.Email);
                return CustomerCreationResult.Rejected();
            }

            logger.LogError(
                response.Error,
                "[{Feature}] Unexpected status {StatusCode} while creating a customer",
                CreateFeature,
                response.StatusCode);

            return CustomerCreationResult.Unavailable();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "[{Feature}] Customer service unreachable while creating a customer", CreateFeature);
            return CustomerCreationResult.Unavailable();
        }
    }

    /// <summary>
    /// Reads the created public id out of the 201 body. The endpoint answers a JSON string
    /// (<c>"cus_…"</c>) but Refit hands back the RAW body for a <c>string</c> result — it does not run
    /// the JSON serialiser — so the quotes must be unwrapped here, otherwise they end up in the URL.
    /// </summary>
    private static string? ReadCreatedPublicId(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        var trimmed = body.Trim();

        if (!trimmed.StartsWith('"'))
        {
            return trimmed;
        }

        try
        {
            return JsonSerializer.Deserialize<string>(trimmed)?.Trim();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
