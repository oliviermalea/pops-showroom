using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.BuildingBlocks.Domain.PublicIds;

public static class PublicIdErrors
{
    public static Error PrefixRequired()
        => Error.Validation(
            "PublicId.PrefixRequired",
            "A prefix is required to generate a public id.");

    public static Error InvalidPrefix(string prefix)
        => Error.Validation(
            "PublicId.InvalidPrefix",
            $"The prefix '{prefix}' is invalid. It must contain exactly 3 lowercase letters.");
}
