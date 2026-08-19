using AwesomeAssertions;
using Bunit;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.Web.Features.Customer;
using ShowRoom.Web.Features.Customer.CreateCustomer;
using ShowRoom.Web.Tests.Doubles;
using Xunit;
using CreatePage = ShowRoom.Web.Features.Customer.CreateCustomer.Page;

namespace ShowRoom.Web.Tests.Features.Customer.CreateCustomer;

/// <summary>
/// Scénarios du formulaire de création. Le validateur réel est injecté (et non simulé) : c'est lui que
/// l'utilisateur subit, et ses messages font partie du comportement observable.
/// </summary>
public sealed class PageTests : BunitContext
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
        page.Find("#firstName").Change("  Ada  ");
        page.Find("#lastName").Change("Lovelace");
        page.Find("#email").Change("ada@showroom.test");
        page.Find("#phone").Change("   ");
    }

    private string CurrentUrl => Services.GetRequiredService<NavigationManager>().Uri;

    [Fact]
    public void An_empty_form_is_refused_before_any_call_to_the_service()
    {
        // Arrange
        var facade = Facade();
        var sut = Render<CreatePage>();

        // Act
        sut.Find("form").Submit();

        // Assert
        sut.FindAll(".validation-message").Select(m => m.TextContent)
            .Should().Contain("Le prénom est obligatoire.")
            .And.Contain("Le nom est obligatoire.")
            .And.Contain("L'email est obligatoire.");
        facade.CallCount.Should().Be(0, "rien ne doit partir tant que la saisie est invalide");
    }

    [Fact]
    public void A_malformed_email_is_refused_with_a_precise_message()
    {
        // Arrange
        var facade = Facade();
        var sut = Render<CreatePage>();
        Fill(sut);
        sut.Find("#email").Change("pas-un-email");

        // Act
        sut.Find("form").Submit();

        // Assert
        sut.FindAll(".validation-message").Select(m => m.TextContent)
            .Should().Contain("Saisissez une adresse email valide.");
        facade.CallCount.Should().Be(0);
    }

    [Fact]
    public void A_valid_form_sends_a_trimmed_payload_without_an_empty_phone()
    {
        // Arrange
        var facade = Facade();
        facade.CreationResult = CustomerCreationResult.Created(CustomerSamples.PublicId);
        var sut = Render<CreatePage>();
        Fill(sut);

        // Act
        sut.Find("form").Submit();

        // Assert
        facade.LastForm.Should().NotBeNull();
        var sent = CreateCustomerMapper.ToApi(facade.LastForm!);
        sent.FirstName.Should().Be("Ada");
        sent.Phone.Should().BeNull();
    }

    [Fact]
    public void A_successful_creation_redirects_to_the_fiche_of_the_new_customer()
    {
        // Arrange
        var facade = Facade();
        facade.CreationResult = CustomerCreationResult.Created(CustomerSamples.PublicId);
        var sut = Render<CreatePage>();
        Fill(sut);

        // Act
        sut.Find("form").Submit();

        // Assert
        CurrentUrl.Should().EndWith($"/customers/{CustomerSamples.PublicId}");
    }

    [Fact]
    public void A_duplicate_email_is_reported_on_the_form_without_leaving_it()
    {
        // Arrange
        var facade = Facade();
        facade.CreationResult = CustomerCreationResult.EmailAlreadyUsed();
        var sut = Render<CreatePage>();
        var urlBefore = CurrentUrl;
        Fill(sut);

        // Act
        sut.Find("form").Submit();

        // Assert
        sut.Find("[role='alert']").TextContent.Should().Contain("utilise déjà").And.Contain("ada@showroom.test");
        CurrentUrl.Should().Be(urlBefore, "l'utilisateur doit pouvoir corriger sa saisie");
    }

    [Fact]
    public void An_unreachable_service_invites_the_user_to_try_again_later()
    {
        // Arrange
        var facade = Facade();
        facade.CreationResult = CustomerCreationResult.Unavailable();
        var sut = Render<CreatePage>();
        Fill(sut);

        // Act
        sut.Find("form").Submit();

        // Assert
        sut.Find("[role='alert']").TextContent.Should().Contain("momentanément indisponible");
    }

    [Fact]
    public void Resetting_the_form_clears_the_saisie_and_the_previous_failure()
    {
        // Arrange
        var facade = Facade();
        facade.CreationResult = CustomerCreationResult.EmailAlreadyUsed();
        var sut = Render<CreatePage>();
        Fill(sut);
        sut.Find("form").Submit();
        sut.FindAll("[role='alert']").Should().NotBeEmpty();

        // Act
        sut.FindAll("button[type='button']").Single(b => b.TextContent.Contains("Réinitialiser")).Click();

        // Assert
        sut.Find("#firstName").GetAttribute("value").Should().BeNullOrEmpty();
        sut.FindAll("[role='alert']").Should().BeEmpty();
    }

    [Fact]
    public void The_form_always_offers_a_way_out()
    {
        // Arrange
        Facade();

        // Act
        var sut = Render<CreatePage>();

        // Assert
        sut.Find("a[href='/customers']").TextContent.Should().Contain("Annuler");
    }
}
