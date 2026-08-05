using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.BuildingBlocks.Http.Errors;

public static class ResultProblemDetailsExtensions
{
    public static ProblemHttpResult ToProblemDetails(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result cannot be converted to ProblemDetails.");
        }

        var firstError = result.FirstError;
        var statusCode = firstError.Category.HttpStatusCode;
        var title = firstError.Category.Title;

        if (firstError.Category == ErrorCategory.Validation)
        {
            var errors = result.Errors
                .GroupBy(x => x.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Message).Distinct().ToArray());

            var validationExtensions = new Dictionary<string, object?>
            {
                ["errors"] = errors
            };

            return TypedResults.Problem(
                title: title,
                type: $"https://www.rfc-editor.org/rfc/rfc9110.html#name-{ToRfcSectionSlug(statusCode)}",
                statusCode: statusCode,
                extensions: validationExtensions);
        }

        var extensions = new Dictionary<string, object?>
        {
            ["errors"] = result.Errors
                .Select(x => new
                {
                    x.Code,
                    x.Message,
                    Category = x.Category.Name
                })
                .ToArray()
        };

        return TypedResults.Problem(
            detail: firstError.Message,
            title: title,
            type: $"https://www.rfc-editor.org/rfc/rfc9110.html#name-{ToRfcSectionSlug(statusCode)}",
            statusCode: statusCode,
            extensions: extensions);
    }

    public static ProblemHttpResult ToProblemDetails<TValue>(this Result<TValue> result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result cannot be converted to ProblemDetails.");
        }

        return ((Result)result).ToProblemDetails();
    }

    private static string ToRfcSectionSlug(int statusCode) => statusCode switch
    {
        400 => "400-bad-request",
        401 => "401-unauthorized",
        403 => "403-forbidden",
        404 => "404-not-found",
        409 => "409-conflict",
        _ => "500-internal-server-error"
    };
}