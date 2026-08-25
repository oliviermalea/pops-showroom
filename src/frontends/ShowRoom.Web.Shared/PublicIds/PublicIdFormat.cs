using System.Text.RegularExpressions;

namespace ShowRoom.Web.Shared.PublicIds;

/// <summary>
/// Boundary check on the public id format (<c>abc_</c> + 32 hex chars) used by every ShowRoom API.
/// Purely defensive: it avoids firing a request the backend would reject with 400, and lets the UI show
/// a precise message. The authority on the format stays the backend.
/// </summary>
public static partial class PublicIdFormat
{
    public static bool IsValid(string? value)
        => !string.IsNullOrWhiteSpace(value) && PublicIdRegex().IsMatch(value.Trim());

    [GeneratedRegex("^[a-z]{3}_[a-f0-9]{32}$", RegexOptions.Compiled)]
    private static partial Regex PublicIdRegex();
}
