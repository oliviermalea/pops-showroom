using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.SharedKernel.PhoneNumbers;

public static class PhoneNumberErrors
{
    public static Error Invalid(string? value) =>
        Error.Validation(
            "PhoneNumber.Invalid",
            $"The phone number is invalid: '{value}'.");
}
