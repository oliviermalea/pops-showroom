namespace ShowRoom.Web.Shared.Api.Problems;

/// <summary>One error carried by a ProblemDetails payload: a stable code and its server-side message.</summary>
/// <param name="Code">
/// The backend's stable error code (<c>Customer.EmailAlreadyExists</c>, <c>Validation.Email</c>…). This
/// is the part of the contract the UI may reason on — the message is a hint, not an interface.
/// </param>
/// <param name="Message">The server-side message, in the API's language. Diagnostic material.</param>
public sealed record ApiProblemError(string Code, string Message);

/// <summary>
/// An RFC 7807 ProblemDetails payload, as the front end consumes it.
/// </summary>
/// <remarks>
/// <para>ShowRoom APIs answer every failure with ProblemDetails, in two shapes produced by
/// <c>ResultProblemDetailsExtensions</c>: validation failures carry <c>errors</c> as an OBJECT keyed by
/// code, other failures carry it as an ARRAY of <c>{code, message, category}</c>. Both are normalised
/// here into a single flat list, so a screen never has to know which shape it got.</para>
///
/// <para>The <see cref="Detail"/> and <see cref="ApiProblemError.Message"/> texts come from the API and
/// are therefore in the API's language (English). They are diagnostic material — a user-facing sentence
/// is resolved from the <see cref="ApiProblemError.Code"/>, which is the stable part of the contract.</para>
/// </remarks>
public sealed record ApiProblem(
    int Status,
    string? Title,
    string? Detail,
    string? TraceId,
    IReadOnlyList<ApiProblemError> Errors)
{
    /// <summary>Prefix the backend gives to errors produced by a FluentValidation rule.</summary>
    private const string ValidationCodePrefix = "Validation.";

    /// <summary>A problem with nothing but a status: the response carried no readable body.</summary>
    public static ApiProblem FromStatus(int status) => new(status, null, null, null, []);

    /// <summary>Distinct error codes, in the order the API returned them.</summary>
    public IReadOnlyList<string> Codes => Errors.Select(error => error.Code).Distinct(StringComparer.Ordinal).ToArray();

    /// <summary>True when the payload carried at least one machine-readable code.</summary>
    public bool HasCodes => Errors.Count > 0;

    /// <summary>Tells whether the API reported this exact error code.</summary>
    public bool Has(string code) => Errors.Any(error => string.Equals(error.Code, code, StringComparison.Ordinal));

    /// <summary>
    /// Validation errors keyed by the field they concern. The backend codes them
    /// <c>Validation.&lt;PropertyName&gt;</c> and its command properties carry the same names as the
    /// form model, so the key is directly usable as a form field name.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> FieldErrors =>
        Errors
            .Where(error => error.Code.StartsWith(ValidationCodePrefix, StringComparison.Ordinal))
            .GroupBy(
                error => error.Code[ValidationCodePrefix.Length..],
                StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(error => error.Message).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);
}
