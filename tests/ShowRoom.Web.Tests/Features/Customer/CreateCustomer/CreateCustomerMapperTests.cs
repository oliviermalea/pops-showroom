using AwesomeAssertions;
using ShowRoom.Web.Features.Customer.CreateCustomer;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Customer.CreateCustomer;

public sealed class CreateCustomerMapperTests
{
    [Fact]
    public void ToApi_maps_every_field()
    {
        // Arrange
        var form = new CreateCustomerForm
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@showroom.test",
            Phone = "+33123456789",
        };

        // Act
        var sut = CreateCustomerMapper.ToApi(form);

        // Assert
        sut.FirstName.Should().Be("Ada");
        sut.LastName.Should().Be("Lovelace");
        sut.Email.Should().Be("ada@showroom.test");
        sut.Phone.Should().Be("+33123456789");
    }

    [Fact]
    public void ToApi_trims_every_input()
    {
        // Arrange
        var form = new CreateCustomerForm
        {
            FirstName = "  Ada  ",
            LastName = "  Lovelace ",
            Email = " ada@showroom.test ",
            Phone = "  +33123456789 ",
        };

        // Act
        var sut = CreateCustomerMapper.ToApi(form);

        // Assert
        sut.FirstName.Should().Be("Ada");
        sut.LastName.Should().Be("Lovelace");
        sut.Email.Should().Be("ada@showroom.test");
        sut.Phone.Should().Be("+33123456789");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToApi_sends_null_rather_than_an_empty_phone(string? phone)
    {
        // Arrange
        var form = new CreateCustomerForm
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@showroom.test",
            Phone = phone,
        };

        // Act
        var sut = CreateCustomerMapper.ToApi(form);

        // Assert
        sut.Phone.Should().BeNull();
    }

    [Fact]
    public void ToApi_rejects_a_null_form()
    {
        // Arrange
        CreateCustomerForm? form = null;

        // Act
        var act = () => CreateCustomerMapper.ToApi(form!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
