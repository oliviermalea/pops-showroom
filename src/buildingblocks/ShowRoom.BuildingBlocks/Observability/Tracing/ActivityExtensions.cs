using System.Diagnostics;

namespace ShowRoom.BuildingBlocks.Observability.Tracing;

/// <summary>
/// Provides extension methods to enrich Activity instances with Module, Feature, and RequestId tags.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Sets common tags on the activity: module, feature, and request.id.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="module">The module name (e.g. "Acquisition").</param>
    /// <param name="feature">The feature name (e.g. "CreateLeadWithInformationRequest").</param>
    /// <param name="requestId">An optional request or correlation id.</param>
    /// <returns>The same activity instance for chaining.</returns>
    public static Activity SetCommonTags(
        this Activity activity,
        string module,
        string feature,
        string? requestId = null)
    {
        if (activity is null)
            return activity!;

        activity
            .SetTag("module", module)
            .SetTag("feature", feature);

        if (!string.IsNullOrWhiteSpace(requestId))
            activity.SetTag("request.id", requestId);

        return activity;
    }

    /// <summary>
    /// Sets common tags on the activity using the type name as the feature.
    /// </summary>
    public static Activity SetCommonTags<T>(
        this Activity activity,
        string module,
        string? requestId = null)
    {
        var feature = typeof(T).Name;
        return activity.SetCommonTags(module, feature, requestId);
    }
}