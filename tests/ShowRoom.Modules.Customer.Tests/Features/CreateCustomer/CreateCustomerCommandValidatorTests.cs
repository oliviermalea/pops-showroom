using AwesomeAssertions;
using ShowRoom.Modules.Customer.Features.CreateCustomer;
using Xunit;

namespace ShowRoom.Modules.Customer.Tests.Features.CreateCustomer;

public sealed class CreateCustomerCommandValidatorTests
{
    private readonly CreateCustomerCommandValidator _sut = new();

    private static CreateCustomerCommand ValidCommand() => new()
    {
        FirstName = "Grace",
        LastName = "Hopper",
        Email = "grace.hopper@example.com",
        Phone = "+33123456789",
    };

    [Fact]
    public void Passes_for_a_valid_command()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Passes_when_phone_is_omitted()
    {
        var result = _sut.Validate(ValidCommand() with { Phone = null });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Hopper", "grace@example.com")]
    [InlineData("Grace", "", "grace@example.com")]
    [InlineData("Grace", "Hopper", "")]
    [InlineData("Grace", "Hopper", "not-an-email")]
    public void Fails_for_invalid_required_fields(string firstName, string lastName, string email)
    {
        var command = new CreateCustomerCommand
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
        };

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
