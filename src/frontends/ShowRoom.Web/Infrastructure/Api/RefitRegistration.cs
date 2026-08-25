using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Http.Resilience;
using Refit;
using ShowRoom.Web.Infrastructure.Api.Refit.Customer;

namespace ShowRoom.Web.Infrastructure.Api;

/// <summary>
/// Registers the Refit-typed backend clients. Every client is created through
/// <c>IHttpClientFactory</c> so it inherits the ServiceDefaults pipeline (service discovery +
/// standard resilience handler) and its outgoing calls join the distributed trace.
/// </summary>
public static class RefitRegistration
{
    public static IServiceCollection AddBackendApis(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(BackendApiOptions.SectionName).Get<BackendApiOptions>()
            ?? throw new InvalidOperationException(
                $"Missing configuration section '{BackendApiOptions.SectionName}'.");

        services.Configure<BackendApiOptions>(configuration.GetSection(BackendApiOptions.SectionName));

        // AddRefitGeneratedClient, not AddRefitClient: since Refit 15 the reflection-based request
        // builder ships in a separate package, and the runtime throws NotSupportedException without it.
        // The generated path is also the one that survives trimming and AOT — which matters the day a
        // client is downloaded into the browser.
        services.AddRefitGeneratedClient<ICustomerApi>(CreateRefitSettings())
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(options.CustomerApiBaseUrl))
            .ConfigureCustomerApiResilience();

        return services;
    }

    /// <summary>
    /// Replaces the ServiceDefaults default resilience pipeline for this client. Rationale: the
    /// aggregating endpoint (<c>/customers/{id}/with-orders</c>) fetches the order history from another
    /// service over AMQP and, when that hop fails, only answers once its own bounded retry budget is
    /// spent — with <c>ordersAvailable = false</c> and HTTP 200. That budget is ~6.3 s measured
    /// (<c>OrderHistoryRetryPolicy</c> in the Customer module).
    /// A per-attempt timeout shorter than that would kill the call first, turning a **partial success
    /// the UI can explain** into a plain "service unavailable". The client budget must therefore outlast
    /// the slowest degradation path it wants to observe — here roughly twice it, leaving headroom for
    /// the network and a broker cold start. Retries are cut to one: re-issuing an aggregation that just
    /// exhausted its own retries adds latency without adding information.
    /// </summary>
    private static IHttpClientBuilder ConfigureCustomerApiResilience(this IHttpClientBuilder builder)
    {
        // ServiceDefaults installs the standard pipeline on every client; it has to be dropped before a
        // client-specific one can replace it, otherwise the two pipelines nest and the shortest timeout
        // still wins. The removal API is flagged experimental by the Extensions team — the suppression
        // is deliberate and scoped to this call.
#pragma warning disable EXTEXP0001
        builder.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

        builder.AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(12);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);

            // Polly requires the breaker's sampling window to cover at least two attempts.
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(24);

            options.Retry.MaxRetryAttempts = 1;
        });

        return builder;
    }

    /// <summary>
    /// Serialisation aligned with the backend: camelCase payloads, tolerant on casing, and no
    /// serialisation of null members on the way out.
    /// </summary>
    private static RefitSettings CreateRefitSettings()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        return new RefitSettings(new SystemTextJsonContentSerializer(jsonOptions));
    }
}
