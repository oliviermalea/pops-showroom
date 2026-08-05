namespace ShowRoom.BuildingBlocks.Results;

/// <summary>
/// Represents the result of an operation, which can either be successful or failed,
/// along with any associated errors.
/// </summary>
public class Result
{
    private readonly List<Error> _errors;

    protected Result(bool isSuccess, IEnumerable<Error>? errors = null)
    {
        var errorList = errors?
            .Where(e => e != Error.None)
            .Distinct()
            .ToList() ?? [];

        if (isSuccess && errorList.Count > 0)
        {
            throw new InvalidOperationException("A successful result cannot contain errors.");
        }

        if (!isSuccess && errorList.Count == 0)
        {
            throw new InvalidOperationException("A failed result must contain at least one error.");
        }

        IsSuccess = isSuccess;
        _errors = errorList;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<Error> Errors => _errors;

    public Error FirstError => IsFailure ? _errors[0] : Error.None;

    public static Result Success() => new(true);

    public static Result Fail(Error error) => new(false, [error]);

    public static Result Fail(IEnumerable<Error> errors) => new(false, errors);

    public static Result SuccessIf(bool condition, Error error) =>
        condition ? Success() : Fail(error);

    public static Result FailIf(bool condition, Error error) =>
        condition ? Fail(error) : Success();

    public T Match<T>(Func<T> onSuccess, Func<IReadOnlyList<Error>, T> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Errors);

    public static implicit operator Result(Error error) => Fail(error);
}
