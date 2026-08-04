using Microsoft.Extensions.Logging;

namespace ShowRoom.BuildingBlocks.Observability.Logging;

/// <summary>
/// Provides extension methods to enrich log scopes with Module, Feature, and RequestId.
/// </summary>
public static class LogContextExtensions
{
    /// <summary>
    /// Begins a log scope enriched with Module, Feature, and an optional RequestId.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="module">The module name (e.g. "Acquisition").</param>
    /// <param name="feature">The feature name (e.g. "CreateLeadWithInformationRequest").</param>
    /// <param name="requestId">An optional request or correlation id. If null, a new GUID is generated.</param>
    /// <returns>A disposable log scope.</returns>
    public static IDisposable BeginModuleScope(
        this ILogger logger,
        string module,
        string feature,
        string? requestId = null)
    {
        requestId ??= Guid.NewGuid().ToString("N");

        return logger.BeginScope(new Dictionary<string, object>
        {
            ["Module"] = module,
            ["Feature"] = feature,
            ["RequestId"] = requestId,
        });
    }

    /// <summary>
    /// Begins a log scope enriched with Module, Feature, and an optional RequestId,
    /// using the type name of the current class as the feature.
    /// </summary>
    public static IDisposable BeginModuleScope<T>(
        this ILogger logger,
        string module,
        string? requestId = null)
    {
        var feature = typeof(T).Name;
        return logger.BeginModuleScope(module, feature, requestId);
    }
}