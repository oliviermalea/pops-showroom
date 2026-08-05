using ShowRoom.BuildingBlocks.Results;
using System.Text.RegularExpressions;

namespace ShowRoom.SharedKernel.Emails;

/// <summary>
/// Validated email value object. Normalised to trimmed lower-case and matched against a standard
/// email pattern. Creation failures are returned as an <see cref="Error"/>.
/// </summary>
public sealed partial record Email
{
    private static readonly Regex Regex = EmailRegex();

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EmailErrors.Invalid(value);
        }

        var normalized = Normalize(value);

        if (!IsValid(normalized))
        {
            return EmailErrors.Invalid(value);
        }

        return Result<Email>.Success(new Email(normalized));
    }

    public static bool IsValid(string value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value);

    public static string Normalize(string value) =>
        value.Trim().ToLowerInvariant();

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[\w-]+(\.[\w-]+)*@([\w-]+\.)+[a-zA-Z]{2,7}$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
