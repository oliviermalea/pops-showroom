using AwesomeAssertions;
using ShowRoom.Web.Features.Customer.CreateCustomer;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Customer.CreateCustomer;

public sealed class CreateCustomerFormValidatorTests
{
    private readonly CreateCustomerFormValidator sut = new();

    [Fact]
    public void A_complete_form_is_valid()
    {
        // Arrange
        var form = CreateForm();

        // Act
        var result = sut.Validate(form);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void A_form_without_phone_is_valid()
    {
        // Arrange
        var form = CreateForm();
        form.Phone = null;

        // Act
        var result = sut.Validate(form);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void First_name_is_required(string value)
    {
        // Arrange
        var form = CreateForm();
        form.FirstName = value;

        // Act
        var result = sut.Validate(form);

        // Assert
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(CreateCustomerForm.FirstName))
            .Which.ErrorMessage.Should().Be("Le prénom est obligatoire.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Last_name_is_required(string value)
    {
        // Arrange
        var form = CreateForm();
        form.LastName = value;

        // Act
        var result = sut.Validate(form);

        // Assert
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(CreateCustomerForm.LastName))
            .Which.ErrorMessage.Should().Be("Le nom est obligatoire.");
    }

    [Fact]
    public void Email_is_required()
    {
        // Arrange
        var form = CreateForm();
        form.Email = string.Empty;

        // Act
        var result = sut.Validate(form);

        // Assert
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateCustomerForm.Email)
            && x.ErrorMessage == "L'email est obligatoire.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("ada@")]
    [InlineData("@showroom.test")]
    public void Email_must_be_well_formed(string value)
    {
        // Arrange
        var form = CreateForm();
        form.Email = value;

        // Act
        var result = sut.Validate(form);

        // Assert
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateCustomerForm.Email)
            && x.ErrorMessage == "Saisissez une adresse email valide.");
    }

    [Fact]
    public void Names_are_capped_at_the_backend_length()
    {
        // Arrange
        var form = CreateForm();
        form.FirstName = new string('a', 201);
        form.LastName = new string('b', 201);

        // Act
        var result = sut.Validate(form);

        // Assert
        result.Errors.Should().Contain(x => x.ErrorMessage == "Le prénom ne doit pas dépasser 200 caractères.");
        result.Errors.Should().Contain(x => x.ErrorMessage == "Le nom ne doit pas dépasser 200 caractères.");
    }

    [Fact]
    public void Phone_is_capped_at_the_backend_length()
    {
        // Arrange
        var form = CreateForm();
        form.Phone = new string('0', 41);

        // Act
        var result = sut.Validate(form);

        // Assert
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(CreateCustomerForm.Phone))
            .Which.ErrorMessage.Should().Be("Le téléphone ne doit pas dépasser 40 caractères.");
    }

    private static CreateCustomerForm CreateForm() => new()
    {
        FirstName = "Ada",
        LastName = "Lovelace",
        Email = "ada@showroom.test",
        Phone = "+33123456789",
    };
}
