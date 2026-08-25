using AwesomeAssertions;
using ShowRoom.Web.Shared.Api.Problems;
using Xunit;

namespace ShowRoom.Web.Tests.Infrastructure.Api.Problems;

/// <summary>
/// The reader faces the two ProblemDetails shapes ShowRoom APIs emit — validation failures key their
/// <c>errors</c> by code, every other failure lists them as objects — plus everything an error path can
/// actually deliver: nothing, an HTML page from a proxy, a truncated payload.
/// </summary>
public sealed class ApiProblemReaderTests
{
    [Fact]
    public void It_reads_a_validation_problem_whose_errors_are_keyed_by_code()
    {
        // Arrange — the shape ResultProblemDetailsExtensions produces for ErrorCategory.Validation.
        const string body = """
        {
          "type": "https://www.rfc-editor.org/rfc/rfc9110.html#name-400-bad-request",
          "title": "Validation Failed",
          "status": 400,
          "traceId": "00-1f2e3d4c5b6a79880123456789abcdef-0123456789abcdef-01",
          "errors": {
            "Validation.Email": ["Email must be a valid email address."],
            "Validation.FirstName": ["First name is required."]
          }
        }
        """;

        // Act
        var sut = ApiProblemReader.Read(400, body);

        // Assert
        sut.Status.Should().Be(400);
        sut.Title.Should().Be("Validation Failed");
        sut.TraceId.Should().Be("00-1f2e3d4c5b6a79880123456789abcdef-0123456789abcdef-01");
        sut.Codes.Should().BeEquivalentTo("Validation.Email", "Validation.FirstName");
        sut.FieldErrors.Should().ContainKey("Email");
        sut.FieldErrors["Email"].Should().ContainSingle().Which.Should().Be("Email must be a valid email address.");
        sut.FieldErrors.Should().ContainKey("FirstName");
    }

    [Fact]
    public void It_reads_a_business_problem_whose_errors_are_a_list_of_objects()
    {
        // Arrange — the shape produced for every non-validation category (404, 409…).
        const string body = """
        {
          "title": "Conflict",
          "status": 409,
          "detail": "A customer with this email already exists.",
          "errors": [
            { "code": "Customer.EmailAlreadyExists", "message": "A customer with this email already exists.", "category": "Conflict" }
          ]
        }
        """;

        // Act
        var sut = ApiProblemReader.Read(409, body);

        // Assert
        sut.Status.Should().Be(409);
        sut.Detail.Should().Be("A customer with this email already exists.");
        sut.Has("Customer.EmailAlreadyExists").Should().BeTrue();
        sut.FieldErrors.Should().BeEmpty("a business error names no form field");
    }

    [Fact]
    public void It_keeps_every_message_when_one_code_carries_several()
    {
        // Arrange
        const string body = """
        { "status": 400, "errors": { "Validation.Email": ["Email is required.", "Email must be valid."] } }
        """;

        // Act
        var sut = ApiProblemReader.Read(400, body);

        // Assert
        sut.FieldErrors["Email"].Should().HaveCount(2);
    }

    [Fact]
    public void It_reads_property_names_whatever_their_casing()
    {
        // Arrange — the contract is the JSON shape, not a serializer's naming policy.
        const string body = """{ "Status": 404, "Title": "Not Found", "Detail": "Nope.", "TraceId": "abc" }""";

        // Act
        var sut = ApiProblemReader.Read(404, body);

        // Assert
        sut.Title.Should().Be("Not Found");
        sut.Detail.Should().Be("Nope.");
        sut.TraceId.Should().Be("abc");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<html><body>502 Bad Gateway</body></html>")]
    [InlineData("{ \"status\": 500, ")]
    [InlineData("[1, 2, 3]")]
    [InlineData("\"just a string\"")]
    public void It_degrades_to_the_bare_status_when_the_body_is_not_a_readable_problem(string? body)
    {
        // Act — an error body is where the unexpected arrives; parsing it must never throw, or a
        // diagnosable failure would turn into an opaque one.
        var sut = ApiProblemReader.Read(503, body);

        // Assert
        sut.Status.Should().Be(503);
        sut.Title.Should().BeNull();
        sut.HasCodes.Should().BeFalse();
        sut.FieldErrors.Should().BeEmpty();
    }

    [Fact]
    public void It_ignores_an_errors_member_of_an_unexpected_shape()
    {
        // Arrange
        const string body = """{ "status": 400, "title": "Bad Request", "errors": "not a collection" }""";

        // Act
        var sut = ApiProblemReader.Read(400, body);

        // Assert — the readable half is kept rather than discarding the whole payload.
        sut.Title.Should().Be("Bad Request");
        sut.HasCodes.Should().BeFalse();
    }

    [Fact]
    public void It_falls_back_to_the_transport_status_when_the_body_declares_none()
    {
        // Arrange
        const string body = """{ "title": "Bad Request" }""";

        // Act
        var sut = ApiProblemReader.Read(400, body);

        // Assert
        sut.Status.Should().Be(400);
    }
}
