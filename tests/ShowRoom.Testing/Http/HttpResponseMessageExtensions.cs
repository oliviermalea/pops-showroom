namespace ShowRoom.Testing.Http;

using System.Text.RegularExpressions;

public static class HttpResponseMessageExtensions
{
    /// <summary>Extracts the resource identifier from the response's Location header.</summary>
    public static string GetIdFromLocationHeader(this HttpResponseMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var location = message.Headers.Location
            ?? throw new InvalidOperationException("The response does not contain a Location header.");

        var lastSegment = location.Segments.LastOrDefault()?.Trim('/');
        if (!string.IsNullOrWhiteSpace(lastSegment))
        {
            return lastSegment;
        }

        var match = Regex.Match(location.ToString(), @"Id=([a-f0-9\-]+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        throw new InvalidOperationException($"Unable to extract an identifier from Location header '{location}'.");
    }
}
