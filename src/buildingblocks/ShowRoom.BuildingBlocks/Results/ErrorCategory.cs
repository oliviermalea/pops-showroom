using Ardalis.SmartEnum;

namespace ShowRoom.BuildingBlocks.Results;

/// <summary>
/// Represents a category of errors that can occur during the execution of an operation,
/// along with their associated HTTP status codes and titles.
/// </summary>
public sealed class ErrorCategory : SmartEnum<ErrorCategory>
{
    public static readonly ErrorCategory Failure =
        new(nameof(Failure), 0, 500, "Server error");

    public static readonly ErrorCategory Validation =
        new(nameof(Validation), 1, 400, "Validation error");

    public static readonly ErrorCategory NotFound =
        new(nameof(NotFound), 2, 404, "Resource not found");

    public static readonly ErrorCategory Conflict =
        new(nameof(Conflict), 3, 409, "Conflict");

    public static readonly ErrorCategory Unauthorized =
        new(nameof(Unauthorized), 4, 401, "Unauthorized");

    public static readonly ErrorCategory Forbidden =
        new(nameof(Forbidden), 5, 403, "Forbidden");

    public int HttpStatusCode { get; }
    public string Title { get; }

    private ErrorCategory(string name, int value, int httpStatusCode, string title)
        : base(name, value)
    {
        HttpStatusCode = httpStatusCode;
        Title = title;
    }
}
