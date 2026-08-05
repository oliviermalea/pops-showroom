using AwesomeAssertions;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.SharedKernel.Emails;
using Xunit;

namespace ShowRoom.Modules.Customer.Tests.Domain;

public sealed class EmailTests
{
    [Theory]
    [InlineData("USER@Example.COM", "user@example.com")]
    [InlineData("  Jane.Doe@Mail.io ", "jane.doe@mail.io")]
    public void Create_normalises_to_trimmed_lowercase(string input, string expected)
    {
        var result = Email.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-symbol")]
    [InlineData("two@@ats.com")]
    [InlineData("trailing@")]
    [InlineData("user@nodomain")]
    public void Create_returns_a_validation_error_for_invalid_values(string input)
    {
        var result = Email.Create(input);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Category.Should().Be(ErrorCategory.Validation);
        result.FirstError.Code.Should().Be("Email.Invalid");
    }
}
