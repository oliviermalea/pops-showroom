using AwesomeAssertions;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Customer.Features.ChangeCustomerEmail;
using Xunit;

namespace ShowRoom.Modules.Customer.Tests.Features.ChangeCustomerEmail;

public sealed class ChangeCustomerEmailCommandValidatorTests
{
    private readonly ChangeCustomerEmailCommandValidator _sut = new();

    private static ChangeCustomerEmailCommand ValidCommand()
        => new(PublicIdFactory.ForCustomer().Value.Value, "new@example.com");

    [Fact]
    public void Valid_command_passes_validation()
    {
        _sut.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Fails_when_public_id_is_malformed()
    {
        _sut.Validate(ValidCommand() with { PublicId = "not-a-public-id" }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@tld")]
    public void Fails_when_email_is_invalid(string email)
    {
        _sut.Validate(ValidCommand() with { NewEmail = email }).IsValid.Should().BeFalse();
    }
}
