using System.Net;
using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Refit;
using ShowRoom.Web.Client.Features.Catalog;
using ShowRoom.Web.Client.Infrastructure.Api;
using ShowRoom.Web.Client.Infrastructure.Api.Refit.Catalog;
using ShowRoom.Web.Client.Infrastructure.Api.Refit.Catalog.Models;
using ShowRoom.Web.Shared.Api.Models;
using Xunit;

namespace ShowRoom.Web.Tests.Features.Catalog;

/// <summary>
/// Orchestration de la façade catalogue : chaque statut HTTP et l'indisponibilité réseau.
/// </summary>
/// <remarks>
/// Cette façade s'exécute des deux côtés — préretour serveur puis navigateur. Ces tests ne peuvent pas
/// distinguer les deux, mais ils garantissent qu'aucun chemin ne laisse échapper d'exception : dans le
/// navigateur, un refus CORS ou un appel bloqué arrive exactement par là.
/// </remarks>
public sealed class CatalogFacadeTests
{
    private const string ValidPublicId = "prd_0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task GetProductsAsync_maps_the_page_when_the_api_answers_200()
    {
        // Arrange
        var api = FakeProductApi.Listing(HttpStatusCode.OK, new PagedResponse<ProductSummaryResponse>(
            [new ProductSummaryResponse("prd_1", "Chaise", 345.5m, "EUR", "Available")], 1, 12, 1, 1));

        var sut = CreateSut(api);

        // Act
        var result = await sut.GetProductsAsync(1, 12, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(ProductListOutcome.Loaded);
        result.Page!.Items.Should().ContainSingle();
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(-5, 12)]
    [InlineData(1, 0)]
    [InlineData(1, 5000)]
    public async Task GetProductsAsync_normalises_the_query_before_calling(int page, int pageSize)
    {
        // Arrange — une query string éditée à la main ne doit jamais produire un 400 du backend.
        var api = FakeProductApi.Listing(HttpStatusCode.OK, new PagedResponse<ProductSummaryResponse>([], 1, 12, 0, 0));
        var sut = CreateSut(api);

        // Act
        await sut.GetProductsAsync(page, pageSize, TestContext.Current.CancellationToken);

        // Assert
        api.LastQuery.Page.Should().BeGreaterThanOrEqualTo(1);
        api.LastQuery.PageSize.Should().BeInRange(1, 100);
    }

    [Fact]
    public async Task GetProductsAsync_degrades_to_unavailable_and_carries_the_problem()
    {
        // Arrange
        var api = FakeProductApi.Failing(HttpStatusCode.InternalServerError, """
        { "title": "Server Error", "status": 500, "traceId": "00-abc-01" }
        """);

        var sut = CreateSut(api);

        // Act
        var result = await sut.GetProductsAsync(1, 12, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(ProductListOutcome.Unavailable);
        result.Problem!.TraceId.Should().Be("00-abc-01");
    }

    [Fact]
    public async Task GetProductsAsync_survives_a_transport_failure()
    {
        // Arrange — dans le navigateur, c'est notamment ce que produit un refus CORS.
        var api = FakeProductApi.Throwing(new HttpRequestException("TypeError: Failed to fetch"));
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetProductsAsync(1, 12, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(ProductListOutcome.Unavailable);
        result.Problem.Should().BeNull("aucun statut n'a été reçu");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("pas-un-id")]
    [InlineData("prd_trop-court")]
    public async Task GetProductAsync_rejects_a_malformed_public_id_without_calling(string? publicId)
    {
        // Arrange
        var api = FakeProductApi.Returning(HttpStatusCode.OK, null);
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetProductAsync(publicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(ProductLookupOutcome.InvalidPublicId);
        api.CallCount.Should().Be(0, "un identifiant mal formé ne mérite pas un aller-retour réseau");
    }

    [Fact]
    public async Task GetProductAsync_returns_the_product_when_the_api_answers_200()
    {
        // Arrange
        var api = FakeProductApi.Returning(
            HttpStatusCode.OK,
            new ProductResponse(ValidPublicId, "Bureau", "Chêne", 1180m, "EUR", "Available"));

        var sut = CreateSut(api);

        // Act
        var result = await sut.GetProductAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(ProductLookupOutcome.Found);
        result.Product!.Name.Should().Be("Bureau");
    }

    [Fact]
    public async Task GetProductAsync_reports_not_found_with_its_business_code()
    {
        // Arrange
        var api = FakeProductApi.Failing(HttpStatusCode.NotFound, """
        { "title": "Not Found", "status": 404, "errors": [ { "code": "Product.NotFound", "message": "…" } ] }
        """);

        var sut = CreateSut(api);

        // Act
        var result = await sut.GetProductAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(ProductLookupOutcome.NotFound);
        result.Problem!.Has("Product.NotFound").Should().BeTrue();
    }

    [Fact]
    public async Task GetProductAsync_treats_a_400_as_an_invalid_public_id()
    {
        // Arrange — le backend reste l'autorité sur le format.
        var api = FakeProductApi.Failing(HttpStatusCode.BadRequest, """{ "status": 400 }""");
        var sut = CreateSut(api);

        // Act
        var result = await sut.GetProductAsync(ValidPublicId, TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(ProductLookupOutcome.InvalidPublicId);
    }

    private static CatalogFacade CreateSut(IProductApi api) => new(
        api,
        Options.Create(new CatalogApiOptions { BusinessApiBaseUrl = "https://localhost:7106", ApiVersion = 1 }),
        NullLogger<CatalogFacade>.Instance);

    private sealed class FakeProductApi : IProductApi
    {
        private readonly HttpStatusCode statusCode;
        private readonly ProductResponse? detail;
        private readonly PagedResponse<ProductSummaryResponse>? page;
        private readonly Exception? exception;
        private readonly string? problemBody;

        private FakeProductApi(
            HttpStatusCode statusCode,
            ProductResponse? detail = null,
            PagedResponse<ProductSummaryResponse>? page = null,
            Exception? exception = null,
            string? problemBody = null)
        {
            this.statusCode = statusCode;
            this.detail = detail;
            this.page = page;
            this.exception = exception;
            this.problemBody = problemBody;
        }

        public int CallCount { get; private set; }

        public (int Page, int PageSize) LastQuery { get; private set; }

        public static FakeProductApi Returning(HttpStatusCode statusCode, ProductResponse? content)
            => new(statusCode, detail: content);

        public static FakeProductApi Listing(HttpStatusCode statusCode, PagedResponse<ProductSummaryResponse>? content)
            => new(statusCode, page: content);

        public static FakeProductApi Failing(HttpStatusCode statusCode, string problemBody)
            => new(statusCode, problemBody: problemBody);

        public static FakeProductApi Throwing(Exception exception)
            => new(HttpStatusCode.OK, exception: exception);

        public Task<ApiResponse<PagedResponse<ProductSummaryResponse>>> GetProductsAsync(
            int version,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastQuery = (page, pageSize);

            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(new ApiResponse<PagedResponse<ProductSummaryResponse>>(
                Respond(), this.page, new RefitSettings(), BuildError()));
        }

        public Task<ApiResponse<ProductResponse>> GetProductByPublicIdAsync(
            int version,
            string publicId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(new ApiResponse<ProductResponse>(
                Respond(), detail, new RefitSettings(), BuildError()));
        }

        /// <summary>
        /// La <see cref="HttpResponseMessage.RequestMessage"/> n'est pas décorative : depuis Refit 15,
        /// <see cref="ApiResponse{T}"/> refuse une réponse sans requête associée, et la doublure qui
        /// l'omet fait échouer la façade en signalant un mauvais outcome plutôt qu'une erreur de type.
        /// </summary>
        private HttpResponseMessage Respond() => new(statusCode)
        {
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7106/api/v1/products"),
        };

        private ApiException? BuildError()
        {
            if (problemBody is null)
            {
                return null;
            }

            var response = Respond();
            response.Content = new StringContent(problemBody, Encoding.UTF8, "application/problem+json");

            return ApiException.Create(
                new HttpRequestMessage(HttpMethod.Get, "https://localhost:7106/api/v1/products"),
                HttpMethod.Get,
                response,
                new RefitSettings()).GetAwaiter().GetResult();
        }
    }
}
