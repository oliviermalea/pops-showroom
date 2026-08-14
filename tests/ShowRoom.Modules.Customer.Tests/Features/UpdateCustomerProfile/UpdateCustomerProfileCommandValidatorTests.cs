using AwesomeAssertions;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Customer.Features.UpdateCustomerProfile;
using Xunit;

namespace ShowRoom.Modules.Customer.Tests.Features.UpdateCustomerProfile;

public sealed class UpdateCustomerProfileCommandValidatorTests
{
    private readonly UpdateCustomerProfileCommandValidator _sut = new();

    private static UpdateCustomerProfileCommand ValidCommand()
        => new(PublicIdFactory.ForCustomer().Value.Value, "Ada", "Lovelace", "ada@example.com", "+33123456789");

    [Fact]
    public void Valid_command_passes_validation()
    {
        _sut.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Passes_when_phone_is_omitted()
    {
        _sut.Validate(ValidCommand() with { Phone = null }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Fails_when_public_id_is_malformed()
    {
        _sut.Validate(ValidCommand() with { PublicId = "not-a-public-id" }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Fails_when_a_name_is_blank(string firstName)
    {
        _sut.Validate(ValidCommand() with { FirstName = firstName }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Fails_when_email_is_invalid()
    {
        _sut.Validate(ValidCommand() with { Email = "not-an-email" }).IsValid.Should().BeFalse();
    }
}
