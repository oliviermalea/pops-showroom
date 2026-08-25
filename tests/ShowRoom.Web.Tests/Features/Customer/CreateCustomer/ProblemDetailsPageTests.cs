using AwesomeAssertions;
using Bunit;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.Web.Features.Customer;
using ShowRoom.Web.Features.Customer.CreateCustomer;
using ShowRoom.Web.Shared.Api.Problems;
using ShowRoom.Web.Tests.Doubles;
using Xunit;
using CreatePage = ShowRoom.Web.Features.Customer.CreateCustomer.Page;

namespace ShowRoom.Web.Tests.Features.Customer.CreateCustomer;

/// <summary>
/// What the API's ProblemDetails changes on the creation screen: a rule only the server can decide no
/// longer lands in a vague banner, it lands on the field it concerns.
/// </summary>
public sealed class ProblemDetailsPageTests : BunitContext
{
    private StubCustomerFacade Facade()
    {
        var facade = new StubCustomerFacade();
        Services.AddScoped<ICustomerFacade>(_ => facade);
        Services.AddScoped<IValidator<CreateCustomerForm>, CreateCustomerFormValidator>();
        return facade;
    }

    private static void Fill(IRenderedComponent<CreatePage> page)
    {
        page.Find("#firstName").Change("Ada");
        page.Find("#lastName").Change("Lovelace");
        page.Find("#email").Change("ada@showroom.test");
    }

    [Fact]
    public void A_server_side_validation_error_is_shown_on_the_field_it_names()
    {
        // Arrange — the client rules pass, the server still rejects the email.
        var facade = Facade();
        facade.CreationResult = CustomerCreationResult.Rejected(new ApiProblem(
            400,
            "Validation Failed",
            null,
            "trace-1",
            [new ApiProblemError("Validation.Email", "Email must be a valid email address.")]));

        var sut = Render<CreatePage>();
        Fill(sut);

        // Act
        sut.Find("form").Submit();

        // Assert — the message is attached to the email field, in the UI language.
        sut.FindAll(".validation-message").Select(message => message.TextContent)
            .Should().Contain("Saisissez une adresse email valide (320 caractères au plus).");

        sut.Find("[role='alert']").TextContent
            .Should().Contain("voyez le détail sous les champs concernés");
    }

    [Fact]
    public void A_duplicate_email_is_reported_on_the_email_field()
    {
        // Arrange — uniqueness is decidable server-side only; the code says which field it concerns.
        var facade = Facade();
        facade.CreationResult = CustomerCreationResult.EmailAlreadyUsed(new ApiProblem(
            409,
            "Conflict",
            "A customer with this email already exists.",
            "trace-2",
            [new ApiProblemError("Customer.EmailAlreadyExists", "Already exists.")]));

        var sut = Render<CreatePage>();
        Fill(sut);

        // Act
        sut.Find("form").Submit();

        // Assert
        sut.FindAll(".validation-message").Select(message => message.TextContent)
            .Should().Contain(message => message.Contains("utilise déjà", StringComparison.Ordinal));

        sut.Find("[role='alert']").TextContent.Should().Contain("ada@showroom.test");
    }

    [Fact]
    public void The_diagnostic_panel_exposes_the_codes_and_the_trace_id()
    {
        // Arrange
        var facade = Facade();
        facade.CreationResult = CustomerCreationResult.Rejected(new ApiProblem(
            400,
            "Validation Failed",
            "One or more fields are invalid.",
            "00-abcdef-0123-01",
            [new ApiProblemError("Validation.Email", "Email must be a valid email address.")]));

        var sut = Render<CreatePage>();
        Fill(sut);

        // Act
        sut.Find("form").Submit();

        // Assert — the server wording stays available for support, without polluting the message.
        var panel = sut.Find("details.problem");
        panel.TextContent.Should().Contain("Validation.Email");
        panel.TextContent.Should().Contain("00-abcdef-0123-01");
        panel.TextContent.Should().Contain("One or more fields are invalid.");
    }

    [Fact]
    public void A_stale_server_error_does_not_block_the_next_submit()
    {
        // Arrange — EditContext.Validate() is false while ANY store holds a message, so a server error
        // left behind would silently freeze the form.
        var facade = Facade();
        facade.CreationResult = CustomerCreationResult.Rejected(new ApiProblem(
            400,
            "Validation Failed",
            null,
            null,
            [new ApiProblemError("Validation.Email", "Email must be a valid email address.")]));

        var sut = Render<CreatePage>();
        Fill(sut);
        sut.Find("form").Submit();
        facade.CallCount.Should().Be(1);

        facade.CreationResult = CustomerCreationResult.Created("cus_0123456789abcdef0123456789abcdef");

        // Act — the user corrects nothing and submits again.
        sut.Find("form").Submit();

        // Assert
        facade.CallCount.Should().Be(2, "the stale server verdict must not veto a new attempt");
    }

    [Fact]
    public void An_unavailable_service_shows_no_diagnostic_panel_when_no_body_came_back()
    {
        // Arrange
        var facade = Facade();
        facade.CreationResult = CustomerCreationResult.Unavailable();

        var sut = Render<CreatePage>();
        Fill(sut);

        // Act
        sut.Find("form").Submit();

        // Assert — nothing technical to show, so nothing is rendered at all.
        sut.Find("[role='alert']").TextContent.Should().Contain("momentanément indisponible");
        sut.FindAll("details.problem").Should().BeEmpty();
    }
}
