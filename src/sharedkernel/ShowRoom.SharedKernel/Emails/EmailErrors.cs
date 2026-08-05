using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.SharedKernel.Emails;

public static class EmailErrors
{
    public static Error Invalid(string? value) =>
        Error.Validation(
            "Email.Invalid",
            $"The email is invalid: '{value}'.");
}
