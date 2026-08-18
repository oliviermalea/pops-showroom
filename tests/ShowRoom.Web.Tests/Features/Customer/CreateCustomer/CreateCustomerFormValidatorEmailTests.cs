using AwesomeAssertions;
using ShowRoom.Web.Features.Customer.CreateCustomer;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Customer.CreateCustomer;

public sealed class CreateCustomerFormValidatorEmailTests
{
    [Fact]
    public void An_empty_email_reports_only_the_required_message()
    {
        // Arrange
        var sut = new CreateCustomerFormValidator();
        var form = new CreateCustomerForm
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = string.Empty,
        };

        // Act
        var result = sut.Validate(form);

        // Assert
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(CreateCustomerForm.Email))
            .Which.ErrorMessage.Should().Be("L'email est obligatoire.");
    }
}
