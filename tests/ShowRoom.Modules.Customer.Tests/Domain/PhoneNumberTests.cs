using AwesomeAssertions;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.SharedKernel.PhoneNumbers;
using Xunit;

namespace ShowRoom.Modules.Customer.Tests.Domain;

public sealed class PhoneNumberTests
{
    [Theory]
    [InlineData("+33123456789", "+33123456789")]
    [InlineData("0033123456789", "0033123456789")]
    [InlineData("01 23 45 67 89", "0123456789")]
    [InlineData("01.23.45.67.89", "0123456789")]
    [InlineData("06-12-34-56-78", "0612345678")]
    public void Create_normalises_and_accepts_valid_french_numbers(string input, string expected)
    {
        var result = PhoneNumber.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("+1 202 555 0143")]   // non-French
    [InlineData("0023456789")]         // starts with 0 then 0
    [InlineData("not-a-number")]
    public void Create_returns_a_validation_error_for_invalid_numbers(string input)
    {
        var result = PhoneNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Category.Should().Be(ErrorCategory.Validation);
        result.FirstError.Code.Should().Be("PhoneNumber.Invalid");
    }
}
