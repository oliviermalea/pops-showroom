using ShowRoom.BuildingBlocks.Results;
using System.Text.RegularExpressions;

namespace ShowRoom.SharedKernel.PhoneNumbers;

/// <summary>
/// Validated French phone number value object. Normalised (spaces/dots/hyphens removed) and matched
/// against the French numbering plan. Creation failures are returned as an <see cref="Error"/>.
/// </summary>
public sealed partial record PhoneNumber
{
    private static readonly Regex Regex = FrenchPhoneNumberRegex();

    private PhoneNumber(string value) => Value = value;

    public string Value { get; }

    public static Result<PhoneNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PhoneNumberErrors.Invalid(value);
        }

        var normalized = Normalize(value);

        if (!IsValid(normalized))
        {
            return PhoneNumberErrors.Invalid(value);
        }

        return Result<PhoneNumber>.Success(new PhoneNumber(normalized));
    }

    public static bool IsValid(string value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value);

    public static string Normalize(string value) =>
        value.Trim()
             .Replace(" ", string.Empty)
             .Replace(".", string.Empty)
             .Replace("-", string.Empty);

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;

    [GeneratedRegex(@"^(?:(?:\+|00)33|0)[1-9]\d{8}$", RegexOptions.Compiled)]
    private static partial Regex FrenchPhoneNumberRegex();
}
