using ShowRoom.BuildingBlocks.Results;
using System.Text.RegularExpressions;

namespace ShowRoom.BuildingBlocks.Domain.PublicIds;

public sealed partial record PublicId
{
    private const int PrefixLength = 3;
    private const int GuidLength = 32;
    private const int TotalLength = PrefixLength + 1 + GuidLength;

    public string Value { get; }

    private PublicId(string value) => Value = value;

    public string Prefix => Value[..PrefixLength];

    public static Result<PublicId> Create(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return PublicIdErrors.PrefixRequired();

        var normalized = NormalizePrefix(prefix);

        if (!PrefixRegex().IsMatch(normalized))
            return PublicIdErrors.InvalidPrefix(prefix);

        var value = $"{normalized}_{Guid.NewGuid():N}";
        return Result<PublicId>.Success(new PublicId(value));
    }

    public static PublicId Parse(string value)
    {
        if (!TryParse(value, out var publicId))
            throw new ArgumentException("Invalid public id format.", nameof(value));

        return publicId!;
    }

    public static bool TryParse(string? value, out PublicId? publicId)
    {
        publicId = null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim();

        if (normalized.Length != TotalLength)
            return false;

        if (!PublicIdRegex().IsMatch(normalized))
            return false;

        publicId = new PublicId(normalized);
        return true;
    }

    public static bool IsValid(string? value)
        => TryParse(value, out _);

    public override string ToString()
        => Value;

    public static implicit operator string(PublicId id)
        => id.Value;

    private static string NormalizePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix is required.", nameof(prefix));

        var normalized = prefix.Trim().ToLowerInvariant();

        if (!PrefixRegex().IsMatch(normalized))
            throw new ArgumentException("Prefix must contain exactly 3 lowercase letters.", nameof(prefix));

        return normalized;
    }

    [GeneratedRegex("^[a-z]{3}$", RegexOptions.Compiled)]
    private static partial Regex PrefixRegex();

    [GeneratedRegex("^[a-z]{3}_[a-f0-9]{32}$", RegexOptions.Compiled)]
    private static partial Regex PublicIdRegex();
}
