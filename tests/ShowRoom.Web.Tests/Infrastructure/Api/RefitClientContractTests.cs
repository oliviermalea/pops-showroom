using System.Net;
using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using ShowRoom.Web.Infrastructure.Api;
using ShowRoom.Web.Infrastructure.Api.Refit.Customer;
using ShowRoom.Web.Infrastructure.Api.Problems;
using Xunit;

namespace ShowRoom.Web.Tests.Infrastructure.Api;

/// <summary>
/// Pins what the front end relies on from <b>Refit itself</b>, through the real production registration.
/// </summary>
/// <remarks>
/// <para>The other unit tests double <c>ICustomerApi</c> by hand and build <c>ApiResponse&lt;T&gt;</c>
/// themselves: they <i>imitate</i> Refit rather than exercise it. That is why the 11 → 15 upgrade
/// surfaced as sixteen business tests failing with a wrong outcome instead of as the type incompatibility
/// it was — the doubles kept compiling while the real client had stopped working.</para>
///
/// <para>These tests go through <see cref="RefitRegistration.AddBackendApis"/>, so they cover the
/// wiring decision too: registering the reflection-based client instead of the generated one throws
/// <c>NotSupportedException</c> at the first call since Refit 15, and that must fail here rather than in
/// a screen. Only the socket is faked.</para>
/// </remarks>
public sealed class RefitClientContractTests
{
    private const string PublicId = "cus_0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task The_registered_client_calls_the_api_and_deserialises_its_answer()
    {
        // Arrange
        var handler = FakeHandler.Answering(HttpStatusCode.OK, """
        {
          "publicId": "cus_0123456789abcdef0123456789abcdef",
          "firstName": "Ada",
          "lastName": "Lovelace",
          "displayName": "Ada Lovelace",
          "email": "ada@showroom.test",
          "phone": null,
          "status": "Active",
          "registeredOn": "2026-08-19T10:00:00+00:00"
        }
        """);

        var sut = CreateClient(handler);

        // Act
        var response = await sut.GetCustomerByPublicIdAsync(1, PublicId, TestContext.Current.CancellationToken);

        // Assert — a generated client that was not generated would have thrown before reaching here.
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Should().NotBeNull();
        response.Content!.DisplayName.Should().Be("Ada Lovelace");
        response.Content.Phone.Should().BeNull();
    }

    [Fact]
    public async Task A_failed_response_keeps_its_body_where_the_problem_reader_looks_for_it()
    {
        // Arrange — this is the exact coupling ApiProblemReader has on Refit: the error body lives on
        // the ApiException hanging off ApiResponse.Error.
        var handler = FakeHandler.Answering(HttpStatusCode.NotFound, """
        { "title": "Not Found", "status": 404, "errors": [ { "code": "Customer.NotFound", "message": "…" } ] }
        """);

        var sut = CreateClient(handler);

        // Act
        var response = await sut.GetCustomerByPublicIdAsync(1, PublicId, TestContext.Current.CancellationToken);
        var problem = ApiProblemReader.From(response);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(404);
        problem.Has("Customer.NotFound").Should().BeTrue("the reader must still find the body Refit attached");
    }

    [Fact]
    public async Task The_route_template_expands_the_version_and_the_query_string()
    {
        // Arrange
        var handler = FakeHandler.Answering(HttpStatusCode.OK, """
        { "items": [], "page": 2, "pageSize": 10, "totalItems": 0, "totalPages": 0 }
        """);

        var sut = CreateClient(handler);

        // Act
        await sut.GetCustomersAsync(1, 2, 10, "ada", TestContext.Current.CancellationToken);

        // Assert — the URL is half of the contract with the backend; a silent change here would only be
        // caught much later, by the integration suite or in production.
        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/api/v1/customers");
        handler.LastRequest.RequestUri.Query.Should().Contain("page=2")
            .And.Contain("pageSize=10")
            .And.Contain("search=ada");
    }

    [Fact]
    public async Task A_body_is_serialised_in_camel_case_like_the_api_expects()
    {
        // Arrange
        var handler = FakeHandler.Answering(HttpStatusCode.Created, $"\"{PublicId}\"");
        var sut = CreateClient(handler);

        // Act
        await sut.CreateCustomerAsync(
            1,
            new ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models.CreateCustomerRequest(
                "Ada",
                "Lovelace",
                "ada@showroom.test",
                null),
            TestContext.Current.CancellationToken);

        // Assert
        handler.LastBody.Should().Contain("\"firstName\":\"Ada\"");
        handler.LastBody.Should().NotContain("\"FirstName\"", "the API binds camelCase properties");
    }

    /// <summary>
    /// Resolves <see cref="ICustomerApi"/> through the production registration, with only the socket
    /// replaced. <c>PostConfigureAll</c> rather than <c>ConfigureHttpClientDefaults</c>: defaults run
    /// BEFORE per-client configuration, and the generated client sets its own primary handler.
    /// </summary>
    private static ICustomerApi CreateClient(FakeHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackendApi:CustomerApiBaseUrl"] = "http://customer-api.test",
                ["BackendApi:ApiVersion"] = "1",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendApis(configuration);
        services.PostConfigureAll<HttpClientFactoryOptions>(options =>
            options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = handler));

        return services.BuildServiceProvider().GetRequiredService<ICustomerApi>();
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private HttpStatusCode statusCode;
        private string body = string.Empty;

        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastBody { get; private set; }

        public static FakeHandler Answering(HttpStatusCode statusCode, string body)
            => new() { statusCode = statusCode, body = body };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;

            if (request.Content is not null)
            {
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(statusCode)
            {
                RequestMessage = request,
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }
}
