using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace ShowRoom.BuildingBlocks.Http.Errors;

/// <summary>
/// Turns a malformed/unreadable request body (<see cref="BadHttpRequestException"/>, e.g. invalid JSON)
/// into a uniform RFC 7807 ProblemDetails <c>400</c> instead of leaking a raw stack trace (the developer
/// exception page in Development) or an opaque 500. Because the handler writes the response and returns
/// <c>true</c>, the exception never propagates to the outer developer exception page, so the clean 400 is
/// returned in every environment. Non-<see cref="BadHttpRequestException"/> failures are left to the
/// default handling.
/// </summary>
public sealed class BadRequestExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException badRequest)
        {
            return false;
        }

        httpContext.Response.StatusCode = badRequest.StatusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = badRequest,
            ProblemDetails =
            {
                Status = badRequest.StatusCode,
                Title = "Bad Request",
                Type = "https://www.rfc-editor.org/rfc/rfc9110.html#name-400-bad-request",
                Detail = "The request body is invalid or could not be read (for example, malformed JSON).",
            },
        });
    }
}
