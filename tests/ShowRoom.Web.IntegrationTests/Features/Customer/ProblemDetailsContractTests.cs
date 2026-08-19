using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using ShowRoom.Web.Infrastructure.Api.Problems;
using ShowRoom.Web.IntegrationTests.Infrastructure;
using Xunit;

namespace ShowRoom.Web.IntegrationTests.Features.Customer;

/// <summary>
/// Pins the ProblemDetails <b>contract</b> between the real API and the front-end reader.
/// </summary>
/// <remarks>
/// <para>Admission criterion for this suite (see <c>.claude/rules/frontend.md</c>): the component tests
/// hand the reader a body written by the test itself, so they prove it parses what we <i>believe</i> the
/// API returns. Only a real host proves what it <i>does</i> return — the casing, the two different
/// shapes of the <c>errors</c> member, the presence of a <c>traceId</c>. A backend reshaping its error
/// payload would leave every unit test green and silently blind the screens.</para>
/// </remarks>
public sealed class ProblemDetailsContractTests(CustomerFrontFixture fixture) : IClassFixture<CustomerFrontFixture>
{
    [Fact]
    public async Task A_validation_failure_carries_errors_keyed_by_Validation_prefixed_codes()
    {
        // Arrange — an empty first name: rejected by the backend validator, not by the transport.
        var payload = new { firstName = "", lastName = "", email = "not-an-email", phone = (string?)null };

        // Act
        var response = await fixture.Api.PostAsJsonAsync(
            "/api/v1/customers",
            payload,
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var sut = ApiProblemReader.Read((int)response.StatusCode, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        sut.Status.Should().Be(400);
        sut.HasCodes.Should().BeTrue("the front end keys its behaviour on the codes, not on the messages");
        sut.Codes.Should().Contain(code => code.StartsWith("Validation.", StringComparison.Ordinal));

        // The whole point of the mapping: a code names a field of the very form that was submitted.
        sut.FieldErrors.Keys.Should().Contain(["FirstName", "LastName", "Email"]);
    }

    [Fact]
    public async Task A_conflict_carries_its_business_code_in_a_list_shaped_errors_member()
    {
        // Arrange — the same email twice: the second call conflicts.
        var email = $"{Guid.NewGuid():N}@showroom.test";
        await fixture.SeedCustomerAsync("Grace", "Hopper", email, TestContext.Current.CancellationToken);

        // Act
        var response = await fixture.Api.PostAsJsonAsync(
            "/api/v1/customers",
            new { firstName = "Grace", lastName = "Hopper", email, phone = (string?)null },
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var sut = ApiProblemReader.Read((int)response.StatusCode, body);

        // Assert — the other shape: errors as a list of {code, message, category}.
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        sut.Status.Should().Be(409);
        sut.Has("Customer.EmailAlreadyExists").Should().BeTrue();
        sut.Detail.Should().NotBeNullOrWhiteSpace();
        sut.FieldErrors.Should().BeEmpty("a business error names no form field");
    }

    [Fact]
    public async Task A_not_found_carries_its_code_and_a_trace_id()
    {
        // Act
        var response = await fixture.Api.GetAsync(
            "/api/v1/customers/cus_0123456789abcdef0123456789abcdef",
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var sut = ApiProblemReader.Read((int)response.StatusCode, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        sut.Has("Customer.NotFound").Should().BeTrue();

        // The trace id is what ties a user-visible failure back to its distributed trace; if the host
        // ever stopped emitting it, the diagnostic panel would quietly lose its most useful field.
        sut.TraceId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task A_malformed_body_still_yields_a_readable_problem_rather_than_a_stack_trace()
    {
        // Arrange — invalid JSON, handled by BadRequestExceptionHandler.
        using var content = new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await fixture.Api.PostAsync(
            "/api/v1/customers",
            content,
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var sut = ApiProblemReader.Read((int)response.StatusCode, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        sut.Title.Should().Be("Bad Request");
        sut.Detail.Should().NotBeNullOrWhiteSpace();
        body.Should().NotContain("at ShowRoom.", "an error payload must never leak a stack trace");
    }

    [Fact]
    public async Task The_error_payload_is_valid_json_whatever_the_status()
    {
        // Act
        var response = await fixture.Api.GetAsync(
            "/api/v1/customers/not-a-public-id",
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert — the reader tolerates a non-JSON body, but the API must never produce one.
        var act = () => JsonDocument.Parse(body);
        act.Should().NotThrow();

        ApiProblemReader.Read((int)response.StatusCode, body).Status.Should().Be((int)response.StatusCode);
    }
}
