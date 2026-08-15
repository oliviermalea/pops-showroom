namespace ShowRoom.Modules.Customer.IntegrationTests.Features.CreateCustomer.Endpoints;

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ShowRoom.Modules.Customer;
using ShowRoom.Modules.Customer.Features.CreateCustomer;
using ShowRoom.Testing.Http;

public class CreateCustomerEndpointTests(CustomerBusinessWebFactory applicationInMemoryFactory)
    : IClassFixture<CustomerBusinessWebFactory>
{
    private static readonly string CustomersRoute = $"/api/v1/{CustomerModule.RouteSegment}";

    private WebApplicationFactory<Program> ConfiguredFactory => applicationInMemoryFactory;

    private static CreateCustomerCommand NewCustomerCommand(string? email = null) => new()
    {
        FirstName = "Grace",
        LastName = "Hopper",
        Email = email ?? $"grace.hopper.{Guid.NewGuid():N}@example.com",
        Phone = "+33123456789",
    };

    [Fact]
    public async Task Should_Create_Customer_And_Return_PublicId()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var command = NewCustomerCommand();

        // Act
        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .PostAsJsonAsync(CustomersRoute, command, cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.Created);
        sut.Headers.Location.Should().NotBeNull();

        var responsePublicId = await sut.Content.ReadFromJsonAsync<string>(cancellationToken);
        responsePublicId.Should().NotBeNullOrWhiteSpace();
        responsePublicId!.Should().StartWith("cus_");

        sut.Headers.Location!.ToString().Should().Contain($"{CustomersRoute}/{responsePublicId}");
        sut.GetIdFromLocationHeader().Should().Be(responsePublicId);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Email_Already_Exists()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var command = NewCustomerCommand(email: $"duplicate.{Guid.NewGuid():N}@example.com");
        var client = ConfiguredFactory.CreateClient();

        var first = await client.PostAsJsonAsync(CustomersRoute, command, cancellationToken);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        HttpResponseMessage sut = await client.PostAsJsonAsync(CustomersRoute, command, cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Command_Is_Invalid()
    {
        var command = NewCustomerCommand() with { Email = "not-an-email" };

        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .PostAsJsonAsync(CustomersRoute, command, CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_Get_Created_Customer_By_Its_PublicId()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var command = NewCustomerCommand();
        var client = ConfiguredFactory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(CustomersRoute, command, cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var publicId = await createResponse.Content.ReadFromJsonAsync<string>(cancellationToken);

        // Act
        HttpResponseMessage sut = await client.GetAsync($"{CustomersRoute}/{publicId}", cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
