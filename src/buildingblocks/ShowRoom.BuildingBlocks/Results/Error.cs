namespace ShowRoom.BuildingBlocks.Results;

/// <summary>
/// Represents an error that can occur during the execution of an operation,
/// including its code, message, and category.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
/// <param name="Category">The category of the error.</param>
public readonly record struct Error(string Code, string Message, ErrorCategory Category)
{
    public static readonly Error None =
        new(string.Empty, string.Empty, ErrorCategory.Failure);

    public static readonly Error NullValue =
        new("General.NullValue", "The specified value is null.", ErrorCategory.Validation);

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorCategory.Validation);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorCategory.NotFound);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorCategory.Conflict);

    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorCategory.Unauthorized);

    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorCategory.Forbidden);

    public static Error Failure(string code, string message) =>
        new(code, message, ErrorCategory.Failure);

    public static Error EmptyId(string? name = null) =>
        new("General.EmptyId", $"{name ?? "Id"} must not be empty.", ErrorCategory.Validation);
}
