using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using ShowRoom.Web.Client.Features.Catalog;
using ShowRoom.Web.Client.Infrastructure.Api.Refit.Catalog;

namespace ShowRoom.Web.Client.Infrastructure.Api;

/// <summary>
/// Registers everything the catalogue screens need. Called from <b>both</b> sides.
/// </summary>
/// <remarks>
/// <para>An <c>InteractiveWebAssembly</c> component runs twice: once on the server while the page is
/// prerendered, then in the browser once the runtime has downloaded. Its dependencies must therefore
/// exist in both containers — the host calls this method too. Having a single registration method is
/// what keeps the two sides from drifting; duplicating it would let a screen work prerendered and fail
/// interactive, or the reverse.</para>
///
/// <para><see cref="AddRefitGeneratedClient"/> rather than <c>AddRefitClient</c>: since Refit 15 the
/// reflection-based request builder ships separately, and it is the wrong trade in a browser anyway —
/// the generated path is the one that survives trimming.</para>
/// </remarks>
public static class CatalogApiRegistration
{
    public static IServiceCollection AddCatalog(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(CatalogApiOptions.SectionName).Get<CatalogApiOptions>()
            ?? throw new InvalidOperationException(
                $"Missing configuration section '{CatalogApiOptions.SectionName}'.");

        services.Configure<CatalogApiOptions>(configuration.GetSection(CatalogApiOptions.SectionName));

        services.AddRefitGeneratedClient<IProductApi>(CreateRefitSettings())
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(options.BusinessApiBaseUrl));

        services.AddScoped<ICatalogFacade, CatalogFacade>();

        return services;
    }

    /// <summary>Serialisation aligned with the backend: camelCase, tolerant on casing.</summary>
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
