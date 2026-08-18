using AwesomeAssertions;
using ShowRoom.Web.Features.Customer.CustomerDetail;
using ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Customer.CustomerDetail;

public sealed class CustomerDetailMapperTests
{
    [Fact]
    public void FromApi_maps_every_property_of_a_complete_response()
    {
        // Arrange
        var response = CreateResponse();

        // Act
        var sut = CustomerDetailMapper.FromApi(response);

        // Assert
        sut.PublicId.Should().Be(response.PublicId);
        sut.DisplayName.Should().Be("Ada Lovelace");
        sut.FirstName.Should().Be("Ada");
        sut.LastName.Should().Be("Lovelace");
        sut.Email.Should().Be("ada@showroom.test");
        sut.Phone.Should().Be("+33123456789");
        sut.Status.Should().Be("Active");
        sut.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromApi_replaces_a_missing_phone_by_the_placeholder(string? phone)
    {
        // Arrange
        var response = CreateResponse() with { Phone = phone };

        // Act
        var sut = CustomerDetailMapper.FromApi(response);

        // Assert
        sut.Phone.Should().Be(CustomerDetailMapper.NotProvided);
    }

    [Fact]
    public void FromApi_falls_back_on_first_and_last_name_when_display_name_is_empty()
    {
        // Arrange
        var response = CreateResponse() with { DisplayName = "  " };

        // Act
        var sut = CustomerDetailMapper.FromApi(response);

        // Assert
        sut.DisplayName.Should().Be("Ada Lovelace");
    }

    [Theory]
    [InlineData("Active", true)]
    [InlineData("active", true)]
    [InlineData("Inactive", false)]
    public void FromApi_derives_the_active_flag_from_the_status(string status, bool expected)
    {
        // Arrange
        var response = CreateResponse() with { Status = status };

        // Act
        var sut = CustomerDetailMapper.FromApi(response);

        // Assert
        sut.IsActive.Should().Be(expected);
    }

    [Fact]
    public void FromApi_formats_the_registration_date_in_french_without_shifting_the_instant()
    {
        // Arrange
        var response = CreateResponse() with
        {
            RegisteredOn = new DateTimeOffset(2026, 3, 9, 22, 45, 0, TimeSpan.Zero),
        };

        // Act
        var sut = CustomerDetailMapper.FromApi(response);

        // Assert
        sut.RegisteredOn.Should().Be("09 mars 2026");
    }

    [Fact]
    public void FromApi_rejects_a_null_response()
    {
        // Arrange
        CustomerDetailResponse? response = null;

        // Act
        var act = () => CustomerDetailMapper.FromApi(response!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static CustomerDetailResponse CreateResponse() => new(
        PublicId: "cus_0123456789abcdef0123456789abcdef",
        FirstName: "Ada",
        LastName: "Lovelace",
        DisplayName: "Ada Lovelace",
        Email: "ada@showroom.test",
        Phone: "+33123456789",
        Status: "Active",
        RegisteredOn: new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero));
}
