using System.Text.Json;
using Refit;

namespace ShowRoom.Web.Infrastructure.Api.Problems;

/// <summary>
/// Reads an RFC 7807 ProblemDetails body out of a failed API response.
/// </summary>
/// <remarks>
/// <para>Defensive by construction: an error body is exactly the place where the unexpected arrives — an
/// empty body, an HTML page from a reverse proxy, a truncated payload. Every failure to parse degrades to
/// <see cref="ApiProblem.FromStatus"/> rather than throwing, because a parser blowing up while handling an
/// error would replace a diagnosable failure with an opaque one.</para>
///
/// <para>Property lookup is case-insensitive: the contract is the JSON shape, not the serializer's naming
/// policy on either side.</para>
/// </remarks>
public static class ApiProblemReader
{
    /// <summary>
    /// Extracts the problem carried by a failed Refit response. Returns <c>null</c> for a successful one.
    /// </summary>
    public static ApiProblem? From(IApiResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        // IApiResponse exposes a NULLABLE status: the object can exist without an HTTP exchange having
        // completed. There is no status to report then, and 0 says exactly that rather than crashing on
        // the cast — this reader runs on the failure path, where nothing may be assumed.
        var status = response.StatusCode is { } statusCode ? (int)statusCode : 0;

        // IApiResponse.Error is typed as ApiExceptionBase, which carries no body; the body lives on the
        // derived ApiException (and on ValidationApiException, which derives from it).
        return Read(status, (response.Error as ApiException)?.Content);
    }

    /// <summary>Parses a ProblemDetails body; falls back to the bare status when it cannot be read.</summary>
    public static ApiProblem Read(int status, string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return ApiProblem.FromStatus(status);
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return ApiProblem.FromStatus(status);
            }

            return new ApiProblem(
                ReadInt(root, "status") ?? status,
                ReadString(root, "title"),
                ReadString(root, "detail"),
                ReadString(root, "traceId"),
                ReadErrors(root));
        }
        catch (JsonException)
        {
            // Not JSON at all (an HTML error page, a truncated body): the status alone still tells the
            // screen what happened, and the raw body is already in the logs through Refit's ApiException.
            return ApiProblem.FromStatus(status);
        }
    }

    /// <summary>
    /// Normalises the two <c>errors</c> shapes the backend emits: an object keyed by code for validation
    /// failures, an array of <c>{code, message}</c> for every other failure.
    /// </summary>
    private static IReadOnlyList<ApiProblemError> ReadErrors(JsonElement root)
    {
        if (!TryGetProperty(root, "errors", out var errors))
        {
            return [];
        }

        return errors.ValueKind switch
        {
            JsonValueKind.Object => ReadKeyedErrors(errors),
            JsonValueKind.Array => ReadListedErrors(errors),
            _ => [],
        };
    }

    private static List<ApiProblemError> ReadKeyedErrors(JsonElement errors)
    {
        var parsed = new List<ApiProblemError>();

        foreach (var entry in errors.EnumerateObject())
        {
            switch (entry.Value.ValueKind)
            {
                case JsonValueKind.Array:
                    parsed.AddRange(entry.Value
                        .EnumerateArray()
                        .Where(message => message.ValueKind == JsonValueKind.String)
                        .Select(message => new ApiProblemError(entry.Name, message.GetString()!)));
                    break;

                case JsonValueKind.String:
                    parsed.Add(new ApiProblemError(entry.Name, entry.Value.GetString()!));
                    break;

                default:
                    break;
            }
        }

        return parsed;
    }

    private static List<ApiProblemError> ReadListedErrors(JsonElement errors)
    {
        var parsed = new List<ApiProblemError>();

        foreach (var entry in errors.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var code = ReadString(entry, "code");

            if (!string.IsNullOrWhiteSpace(code))
            {
                parsed.Add(new ApiProblemError(code, ReadString(entry, "message") ?? string.Empty));
            }
        }

        return parsed;
    }

    private static string? ReadString(JsonElement element, string name)
        => TryGetProperty(element, name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadInt(JsonElement element, string name)
        => TryGetProperty(element, name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out var parsed)
                ? parsed
                : null;

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
