using FluentValidation.Results;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.BuildingBlocks.Application.Validations;

/// <summary>
/// Bridges FluentValidation's <see cref="ValidationResult"/> to the application's Result/Error types.
/// </summary>
public static class FluentValidationExtensions
{
    public static IReadOnlyList<Error> ToErrors(this ValidationResult validationResult)
        => validationResult.Errors
            .Where(x => x is not null)
            .Select(x => Error.Validation(
                string.IsNullOrWhiteSpace(x.PropertyName)
                    ? "Validation.General"
                    : $"Validation.{x.PropertyName}",
                x.ErrorMessage))
            .Distinct()
            .ToArray();

    public static Result ToResult(this ValidationResult validationResult)
    {
        var errors = validationResult.ToErrors();
        return errors.Count == 0 ? Result.Success() : Result.Fail(errors);
    }

    public static Result<T> ToResult<T>(this ValidationResult validationResult, T value)
    {
        var errors = validationResult.ToErrors();
        return errors.Count == 0 ? Result<T>.Success(value) : Result<T>.Fail(errors);
    }
}
