using System.Net;
using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Refit;
using ShowRoom.Web.Features.Customer;
using ShowRoom.Web.Features.Customer.CreateCustomer;
using ShowRoom.Web.Infrastructure.Api;
using ShowRoom.Web.Infrastructure.Api.Refit.Customer;
using ShowRoom.Web.Infrastructure.Api.Refit.Customer.Models;
using ShowRoom.Web.Infrastructure.Api.Refit.Models;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Customer;

public sealed class CustomerFacadeTests
{
    private const string ValidPublicId = "cus_0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task GetCustomerAsync_returns_the_mapped_customer_when_the_api_answers_200()
    {
        // Arrange
        var api = FakeCustomerApi.Returning(HttpStatusCode.OK, CreateResponse());
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomerAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerLookupOutcome.Found);
        result.Customer.Should().NotBeNull();
        result.Customer!.DisplayName.Should().Be("Ada Lovelace");
        api.LastPublicId.Should().Be(ValidPublicId);
        api.LastVersion.Should().Be(1);
    }

    [Fact]
    public async Task GetCustomerAsync_returns_not_found_when_the_api_answers_404()
    {
        // Arrange
        var api = FakeCustomerApi.Returning(HttpStatusCode.NotFound, content: null);
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomerAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerLookupOutcome.NotFound);
        result.Customer.Should().BeNull();
    }

    [Fact]
    public async Task GetCustomerAsync_returns_invalid_public_id_when_the_api_answers_400()
    {
        // Arrange
        var api = FakeCustomerApi.Returning(HttpStatusCode.BadRequest, content: null);
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomerAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerLookupOutcome.InvalidPublicId);
    }

    [Fact]
    public async Task GetCustomerAsync_degrades_to_unavailable_when_the_api_answers_500()
    {
        // Arrange
        var api = FakeCustomerApi.Returning(HttpStatusCode.InternalServerError, content: null);
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomerAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerLookupOutcome.Unavailable);
    }

    [Fact]
    public async Task GetCustomerAsync_degrades_to_unavailable_when_the_service_is_unreachable()
    {
        // Arrange
        var api = FakeCustomerApi.Throwing(new HttpRequestException("connection refused"));
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomerAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerLookupOutcome.Unavailable);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-public-id")]
    [InlineData("cus_short")]
    [InlineData("CUS_0123456789ABCDEF0123456789ABCDEF")]
    public async Task GetCustomerAsync_rejects_a_malformed_public_id_without_calling_the_api(string? publicId)
    {
        // Arrange
        var api = FakeCustomerApi.Returning(HttpStatusCode.OK, CreateResponse());
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomerAsync(publicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerLookupOutcome.InvalidPublicId);
        api.CallCount.Should().Be(0);
    }

    /// <summary>
    /// The API answers a JSON string and Refit hands the RAW body back for a <c>string</c> result, so
    /// the payload really arrives quoted. Both shapes must yield a clean public id — a leftover quote
    /// would leak into the redirect URL.
    /// </summary>
    [Theory]
    [InlineData("\"cus_0123456789abcdef0123456789abcdef\"")]
    [InlineData("cus_0123456789abcdef0123456789abcdef")]
    [InlineData("  \"cus_0123456789abcdef0123456789abcdef\"  ")]
    public async Task CreateCustomerAsync_unwraps_the_created_public_id_when_the_api_answers_201(string body)
    {
        // Arrange
        var api = FakeCustomerApi.Creating(HttpStatusCode.Created, body);
        var sut = CreateSut(api);

        // Act
        var result = await sut.CreateCustomerAsync(CreateForm(), TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerCreationOutcome.Created);
        result.PublicId.Should().Be(ValidPublicId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateCustomerAsync_degrades_when_the_201_carries_no_public_id(string? body)
    {
        // Arrange
        var api = FakeCustomerApi.Creating(HttpStatusCode.Created, body);
        var sut = CreateSut(api);

        // Act
        var result = await sut.CreateCustomerAsync(CreateForm(), TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerCreationOutcome.Unavailable);
        result.PublicId.Should().BeNull();
    }

    [Fact]
    public async Task CreateCustomerAsync_sends_a_trimmed_payload_without_an_empty_phone()
    {
        // Arrange
        var api = FakeCustomerApi.Creating(HttpStatusCode.Created, $"\"{ValidPublicId}\"");
        var sut = CreateSut(api);
        var form = CreateForm();
        form.FirstName = "  Ada ";
        form.Phone = "   ";

        // Act
        await sut.CreateCustomerAsync(form, TestContext.Current.CancellationToken);

        // Assert
        api.LastRequest.Should().NotBeNull();
        api.LastRequest!.FirstName.Should().Be("Ada");
        api.LastRequest.Phone.Should().BeNull();
    }

    [Fact]
    public async Task CreateCustomerAsync_reports_a_duplicate_email_when_the_api_answers_409()
    {
        // Arrange
        var api = FakeCustomerApi.Creating(HttpStatusCode.Conflict, content: null);
        var sut = CreateSut(api);

        // Act
        var result = await sut.CreateCustomerAsync(CreateForm(), TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerCreationOutcome.EmailAlreadyUsed);
        result.PublicId.Should().BeNull();
    }

    [Fact]
    public async Task CreateCustomerAsync_reports_a_rejection_when_the_api_answers_400()
    {
        // Arrange
        var api = FakeCustomerApi.Creating(HttpStatusCode.BadRequest, content: null);
        var sut = CreateSut(api);

        // Act
        var result = await sut.CreateCustomerAsync(CreateForm(), TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerCreationOutcome.Rejected);
    }

    [Fact]
    public async Task CreateCustomerAsync_degrades_to_unavailable_when_the_service_is_unreachable()
    {
        // Arrange
        var api = FakeCustomerApi.Throwing(new HttpRequestException("connection refused"));
        var sut = CreateSut(api);

        // Act
        var result = await sut.CreateCustomerAsync(CreateForm(), TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerCreationOutcome.Unavailable);
    }

    [Fact]
    public async Task GetCustomersAsync_returns_the_mapped_page_when_the_api_answers_200()
    {
        // Arrange
        var api = FakeCustomerApi.Listing(HttpStatusCode.OK, CreatePage());
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomersAsync(2, 20, "ada", TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerListOutcome.Loaded);
        result.Page.Should().NotBeNull();
        result.Page!.Items.Should().ContainSingle();
        api.LastQuery.Should().Be((2, 20, "ada"));
    }

    [Theory]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-5, 20, 1, 20)]
    [InlineData(1, 0, 1, CustomerFacade.DefaultPageSize)]
    [InlineData(1, 1000, 1, CustomerFacade.DefaultPageSize)]
    public async Task GetCustomersAsync_normalises_out_of_range_paging(
        int page,
        int pageSize,
        int expectedPage,
        int expectedPageSize)
    {
        // Arrange
        var api = FakeCustomerApi.Listing(HttpStatusCode.OK, CreatePage());
        var sut = CreateSut(api);

        // Act
        await sut.GetCustomersAsync(page, pageSize, search: null, TestContext.Current.CancellationToken);

        // Assert
        api.LastQuery.Should().Be((expectedPage, expectedPageSize, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCustomersAsync_omits_a_blank_search(string? search)
    {
        // Arrange
        var api = FakeCustomerApi.Listing(HttpStatusCode.OK, CreatePage());
        var sut = CreateSut(api);

        // Act
        await sut.GetCustomersAsync(1, 20, search, TestContext.Current.CancellationToken);

        // Assert
        api.LastQuery.Search.Should().BeNull();
    }

    [Fact]
    public async Task GetCustomersAsync_trims_the_search_term()
    {
        // Arrange
        var api = FakeCustomerApi.Listing(HttpStatusCode.OK, CreatePage());
        var sut = CreateSut(api);

        // Act
        await sut.GetCustomersAsync(1, 20, "  ada  ", TestContext.Current.CancellationToken);

        // Assert
        api.LastQuery.Search.Should().Be("ada");
    }

    [Fact]
    public async Task GetCustomersAsync_degrades_to_unavailable_when_the_service_is_unreachable()
    {
        // Arrange
        var api = FakeCustomerApi.Throwing(new HttpRequestException("connection refused"));
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomersAsync(1, 20, search: null, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerListOutcome.Unavailable);
        result.Page.Should().BeNull();
    }

    [Fact]
    public async Task GetCustomersAsync_degrades_to_unavailable_when_the_api_answers_500()
    {
        // Arrange
        var api = FakeCustomerApi.Listing(HttpStatusCode.InternalServerError, content: null);
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomersAsync(1, 20, search: null, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerListOutcome.Unavailable);
    }

    [Fact]
    public async Task GetCustomerOrdersAsync_returns_the_history_when_the_api_answers_200()
    {
        // Arrange
        var api = FakeCustomerApi.WithOrders(HttpStatusCode.OK, CreateWithOrders(ordersAvailable: true));
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomerOrdersAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerLookupOutcome.Found);
        result.Customer!.OrdersAvailable.Should().BeTrue();
        result.Customer.Orders.Should().ContainSingle();
    }

    /// <summary>
    /// Partial success: the backend degraded gracefully (Order service unreachable over the bus). The
    /// facade must relay it as Found + OrdersAvailable=false, never as Unavailable.
    /// </summary>
    [Fact]
    public async Task GetCustomerOrdersAsync_relays_the_degraded_history_as_a_partial_success()
    {
        // Arrange
        var api = FakeCustomerApi.WithOrders(HttpStatusCode.OK, CreateWithOrders(ordersAvailable: false));
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomerOrdersAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerLookupOutcome.Found);
        result.Customer!.OrdersAvailable.Should().BeFalse();
        result.Customer.DisplayName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public async Task GetCustomerOrdersAsync_returns_not_found_when_the_api_answers_404()
    {
        // Arrange
        var api = FakeCustomerApi.WithOrders(HttpStatusCode.NotFound, content: null);
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomerOrdersAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerLookupOutcome.NotFound);
    }

    [Fact]
    public async Task GetCustomerOrdersAsync_degrades_to_unavailable_when_the_service_is_unreachable()
    {
        // Arrange
        var api = FakeCustomerApi.Throwing(new HttpRequestException("connection refused"));
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomerOrdersAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerLookupOutcome.Unavailable);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-public-id")]
    public async Task GetCustomerOrdersAsync_rejects_a_malformed_public_id_without_calling_the_api(string? publicId)
    {
        // Arrange
        var api = FakeCustomerApi.WithOrders(HttpStatusCode.OK, CreateWithOrders(ordersAvailable: true));
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomerOrdersAsync(publicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerLookupOutcome.InvalidPublicId);
        api.CallCount.Should().Be(0);
    }

    private static CustomerWithOrdersResponse CreateWithOrders(bool ordersAvailable) => new(
        PublicId: ValidPublicId,
        FirstName: "Ada",
        LastName: "Lovelace",
        DisplayName: "Ada Lovelace",
        Email: "ada@showroom.test",
        Phone: null,
        Status: "Active",
        RegisteredOn: new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero),
        OrdersAvailable: ordersAvailable,
        Orders: ordersAvailable
            ?
            [
                new OrderHistoryLineResponse(
                    OrderPublicId: "ord_0123456789abcdef0123456789abcdef",
                    Status: "Confirmed",
                    Currency: "EUR",
                    OrderDate: new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero),
                    TotalAmount: 25m,
                    Lines:
                    [
                        new OrderLineDetailResponse("prd_0123456789abcdef0123456789abcdef", "Clavier", 2, 12.5m, 25m),
                    ]),
            ]
            : []);

    private static PagedResponse<CustomerSummaryResponse> CreatePage() => new(
        Items:
        [
            new CustomerSummaryResponse(
                PublicId: ValidPublicId,
                FirstName: "Ada",
                LastName: "Lovelace",
                DisplayName: "Ada Lovelace",
                Email: "ada@showroom.test",
                Status: "Active",
                RegisteredOn: new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero)),
        ],
        Page: 2,
        PageSize: 20,
        TotalItems: 21,
        TotalPages: 2);

    private static CreateCustomerForm CreateForm() => new()
    {
        FirstName = "Ada",
        LastName = "Lovelace",
        Email = "ada@showroom.test",
        Phone = "+33123456789",
    };

    private static CustomerFacade CreateSut(ICustomerApi api) => new(
        api,
        Options.Create(new BackendApiOptions { CustomerApiBaseUrl = "http://localhost:5205", ApiVersion = 1 }),
        NullLogger<CustomerFacade>.Instance);

    private static CustomerDetailResponse CreateResponse() => new(
        PublicId: ValidPublicId,
        FirstName: "Ada",
        LastName: "Lovelace",
        DisplayName: "Ada Lovelace",
        Email: "ada@showroom.test",
        Phone: "+33123456789",
        Status: "Active",
        RegisteredOn: new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero));

    /// <summary>Hand-written stub of the Refit interface — no mocking framework needed.</summary>
    [Fact]
    public async Task GetCustomerAsync_carries_the_problem_details_returned_with_a_404()
    {
        // Arrange
        var api = FakeCustomerApi.Failing(
            HttpStatusCode.NotFound,
            """
            {
              "title": "Not Found",
              "status": 404,
              "detail": "No customer was found with public id 'cus_0123456789abcdef0123456789abcdef'.",
              "traceId": "00-abcdef0123456789abcdef0123456789-0123456789abcdef-01",
              "errors": [ { "code": "Customer.NotFound", "message": "No customer was found.", "category": "NotFound" } ]
            }
            """);

        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomerAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert — the outcome still drives the screen; the problem adds what only the server knows.
        result.Outcome.Should().Be(CustomerLookupOutcome.NotFound);
        result.Problem.Should().NotBeNull();
        result.Problem!.Status.Should().Be(404);
        result.Problem.Has("Customer.NotFound").Should().BeTrue();
        result.Problem.TraceId.Should().Be("00-abcdef0123456789abcdef0123456789-0123456789abcdef-01");
    }

    [Fact]
    public async Task CreateCustomerAsync_exposes_the_server_side_validation_errors_by_field()
    {
        // Arrange — the validation shape: errors keyed by "Validation.<PropertyName>".
        var api = FakeCustomerApi.Failing(
            HttpStatusCode.BadRequest,
            """
            {
              "title": "Validation Failed",
              "status": 400,
              "errors": {
                "Validation.Email": ["Email must be a valid email address."],
                "Validation.FirstName": ["First name is required."]
              }
            }
            """);

        var sut = CreateSut(api);

        // Act
        var result = await sut.CreateCustomerAsync(
            new CreateCustomerForm { FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerCreationOutcome.Rejected);
        result.Problem!.FieldErrors.Should().ContainKeys("Email", "FirstName");
    }

    [Fact]
    public async Task CreateCustomerAsync_carries_the_conflict_code_when_the_email_is_taken()
    {
        // Arrange
        var api = FakeCustomerApi.Failing(
            HttpStatusCode.Conflict,
            """
            {
              "title": "Conflict",
              "status": 409,
              "detail": "A customer with this email already exists.",
              "errors": [ { "code": "Customer.EmailAlreadyExists", "message": "Already exists.", "category": "Conflict" } ]
            }
            """);

        var sut = CreateSut(api);

        // Act
        var result = await sut.CreateCustomerAsync(
            new CreateCustomerForm { FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerCreationOutcome.EmailAlreadyUsed);
        result.Problem!.Has("Customer.EmailAlreadyExists").Should().BeTrue();
    }

    [Fact]
    public async Task A_failure_without_a_problem_body_still_yields_its_status()
    {
        // Arrange — a proxy answering 503 with nothing readable must not break the facade.
        var api = FakeCustomerApi.Failing(HttpStatusCode.ServiceUnavailable, "<html>503</html>");
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetCustomerAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(CustomerLookupOutcome.Unavailable);
        result.Problem!.Status.Should().Be(503);
        result.Problem.HasCodes.Should().BeFalse();
    }

    private sealed class FakeCustomerApi : ICustomerApi
    {
        private readonly HttpStatusCode statusCode;
        private readonly CustomerDetailResponse? detail;
        private readonly string? createdPublicId;
        private readonly PagedResponse<CustomerSummaryResponse>? page;
        private readonly CustomerWithOrdersResponse? withOrders;
        private readonly Exception? exception;
        private readonly string? problemBody;

        private FakeCustomerApi(
            HttpStatusCode statusCode,
            CustomerDetailResponse? detail,
            string? createdPublicId,
            PagedResponse<CustomerSummaryResponse>? page,
            CustomerWithOrdersResponse? withOrders,
            Exception? exception,
            string? problemBody = null)
        {
            this.statusCode = statusCode;
            this.detail = detail;
            this.createdPublicId = createdPublicId;
            this.page = page;
            this.withOrders = withOrders;
            this.exception = exception;
            this.problemBody = problemBody;
        }

        public int CallCount { get; private set; }

        public string? LastPublicId { get; private set; }

        public int LastVersion { get; private set; }

        public CreateCustomerRequest? LastRequest { get; private set; }

        public (int Page, int PageSize, string? Search) LastQuery { get; private set; }

        public static FakeCustomerApi Returning(HttpStatusCode statusCode, CustomerDetailResponse? content)
            => new(statusCode, content, createdPublicId: null, page: null, withOrders: null, exception: null);

        /// <summary>Answers <paramref name="statusCode"/> with a real RFC 7807 body, like the API does.</summary>
        public static FakeCustomerApi Failing(HttpStatusCode statusCode, string problemBody)
            => new(statusCode, detail: null, createdPublicId: null, page: null, withOrders: null, exception: null, problemBody);

        public static FakeCustomerApi Creating(HttpStatusCode statusCode, string? content)
            => new(statusCode, detail: null, content, page: null, withOrders: null, exception: null);

        public static FakeCustomerApi Listing(HttpStatusCode statusCode, PagedResponse<CustomerSummaryResponse>? content)
            => new(statusCode, detail: null, createdPublicId: null, content, withOrders: null, exception: null);

        public static FakeCustomerApi WithOrders(HttpStatusCode statusCode, CustomerWithOrdersResponse? content)
            => new(statusCode, detail: null, createdPublicId: null, page: null, content, exception: null);

        public static FakeCustomerApi Throwing(Exception exception)
            => new(HttpStatusCode.OK, detail: null, createdPublicId: null, page: null, withOrders: null, exception);

        public Task<ApiResponse<CustomerWithOrdersResponse>> GetCustomerWithOrdersAsync(
            int version,
            string publicId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastVersion = version;
            LastPublicId = publicId;

            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(
                new ApiResponse<CustomerWithOrdersResponse>(
                    new HttpResponseMessage(statusCode),
                    withOrders,
                    new RefitSettings(),
                    BuildError()));
        }

        public Task<ApiResponse<PagedResponse<CustomerSummaryResponse>>> GetCustomersAsync(
            int version,
            int page,
            int pageSize,
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastVersion = version;
            LastQuery = (page, pageSize, search);

            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(
                new ApiResponse<PagedResponse<CustomerSummaryResponse>>(
                    new HttpResponseMessage(statusCode),
                    this.page,
                    new RefitSettings(),
                    BuildError()));
        }

        public Task<ApiResponse<CustomerDetailResponse>> GetCustomerByPublicIdAsync(
            int version,
            string publicId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastVersion = version;
            LastPublicId = publicId;

            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(
                new ApiResponse<CustomerDetailResponse>(
                    new HttpResponseMessage(statusCode),
                    detail,
                    new RefitSettings(),
                    BuildError()));
        }

        public Task<ApiResponse<string>> CreateCustomerAsync(
            int version,
            CreateCustomerRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastVersion = version;
            LastRequest = request;

            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(
                new ApiResponse<string>(
                    new HttpResponseMessage(statusCode),
                    createdPublicId,
                    new RefitSettings(),
                    BuildError()));
        }

        /// <summary>
        /// Builds the <see cref="ApiException"/> Refit attaches to a failed <see cref="ApiResponse{T}"/>,
        /// so the facade reads the problem body through exactly the same path as in production.
        /// </summary>
        private ApiException? BuildError()
        {
            if (problemBody is null)
            {
                return null;
            }

            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(problemBody, Encoding.UTF8, "application/problem+json"),
            };

            return ApiException.Create(
                new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/v1/customers"),
                HttpMethod.Get,
                response,
                new RefitSettings()).GetAwaiter().GetResult();
        }
    }
}
