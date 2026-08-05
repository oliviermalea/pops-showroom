namespace ShowRoom.BuildingBlocks.Results;

/// <summary>
/// Represents the result of an operation that can either be successful or failed,
/// along with any associated errors and a value of type <typeparamref name="TValue"/>.
/// </summary>
/// <typeparam name="TValue">The type of the value associated with a successful result.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue? value, bool isSuccess, IEnumerable<Error>? errors = null)
        : base(isSuccess, errors)
    {
        _value = value;
    }

    public TValue Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    public TValue? ValueOrDefault => _value;

    public static Result<TValue> Success(TValue value)
    {
        if (value is null)
        {
            return Fail(Error.NullValue);
        }

        return new Result<TValue>(value, true);
    }

    public static new Result<TValue> Fail(Error error) => new(default, false, [error]);

    public static new Result<TValue> Fail(IEnumerable<Error> errors) => new(default, false, errors);

    public static Result<TValue> Create(TValue? value) =>
        value is not null ? Success(value) : Fail(Error.NullValue);

    public static Result<TValue> SuccessIf(
        TValue value,
        Func<TValue, bool> predicate,
        Error error) =>
        predicate(value) ? Success(value) : Fail(error);

    public static Result<TValue> FailIf(
        TValue value,
        Func<TValue, bool> predicate,
        Error error) =>
        predicate(value) ? Fail(error) : Success(value);

    public static Result<TValue> Combine(params Result<TValue>[] results)
    {
        var errors = results
            .Where(x => x.IsFailure)
            .SelectMany(x => x.Errors)
            .Distinct()
            .ToArray();

        return errors.Length == 0
            ? Fail(Error.Failure("General.Combine.Empty", "No successful value could be selected."))
            : Fail(errors);
    }

    public T Match<T>(Func<TValue, T> onSuccess, Func<IReadOnlyList<Error>, T> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Errors);

    public static implicit operator Result<TValue>(Error error) => Fail(error);
}
