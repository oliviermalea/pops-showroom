using AwesomeAssertions;
using ShowRoom.Web.Features.Customer;
using ShowRoom.Web.Infrastructure.Api.Problems;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Customer;

/// <summary>
/// The UI resolves its sentences from the error CODE, not from the server text: the code is the stable
/// half of the contract, the text is the API's own wording in the API's language.
/// </summary>
public sealed class CustomerProblemMessagesTests
{
    [Fact]
    public void It_resolves_the_first_known_code_of_a_problem()
    {
        // Arrange — an unknown code first, so the search must not stop at it.
        var problem = new ApiProblem(409, "Conflict", "A customer with this email already exists.", null,
        [
            new ApiProblemError("Customer.SomethingWeDoNotKnowYet", "…"),
            new ApiProblemError("Customer.EmailAlreadyExists", "…"),
        ]);

        // Act
        var sut = CustomerProblemMessages.Resolve(problem, "fallback");

        // Assert
        sut.Should().Be("Un client utilise déjà cette adresse email.");
    }

    [Fact]
    public void It_falls_back_to_the_caller_sentence_rather_than_to_the_server_text()
    {
        // Arrange — the detail is English and backend-flavoured: it belongs to the diagnostic panel.
        var problem = new ApiProblem(500, "Server Error", "Object reference not set to an instance.", null,
            [new ApiProblemError("Some.Unmapped.Code", "boom")]);

        // Act
        var sut = CustomerProblemMessages.Resolve(problem, "Le service est momentanément indisponible.");

        // Assert
        sut.Should().Be("Le service est momentanément indisponible.");
    }

    [Fact]
    public void It_falls_back_when_there_is_no_problem_at_all()
    {
        // Act
        var sut = CustomerProblemMessages.Resolve(null, "fallback");

        // Assert
        sut.Should().Be("fallback");
    }

    [Fact]
    public void A_field_message_prefers_the_local_wording_over_the_server_one()
    {
        // Act
        var sut = CustomerProblemMessages.ForField("Email", "Email must be a valid email address.");

        // Assert
        sut.Should().Be("Saisissez une adresse email valide (320 caractères au plus).");
    }

    [Fact]
    public void A_field_message_keeps_the_server_wording_when_the_field_is_unknown()
    {
        // Act — on a field, an English sentence still tells the user what to fix; a generic one would not.
        var sut = CustomerProblemMessages.ForField("Nickname", "Nickname must not exceed 10 characters.");

        // Assert
        sut.Should().Be("Nickname must not exceed 10 characters.");
    }
}
