namespace ShowRoom.Modules.Customer.IntegrationTests.Features.GetCustomerByPublicId.Endpoints;

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Customer;
using ShowRoom.SharedKernel.Emails;
using ShowRoom.Modules.Customer.Features.GetCustomerByPublicId;
using ShowRoom.Modules.Customer.Persistence;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

public class GetCustomerByPublicIdEndpointTests(CustomerBusinessWebFactory applicationInMemoryFactory)
    : IClassFixture<CustomerBusinessWebFactory>
{
    private static readonly string CustomersRoute = $"/api/v1/{CustomerModule.RouteSegment}";

    private WebApplicationFactory<Program> ConfiguredFactory => applicationInMemoryFactory;

    [Fact]
    public async Task Should_Get_Customer_By_PublicId()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var factory = ConfiguredFactory;
        var seeded = await SeedCustomerAsync(factory, "Grace", "Hopper", "grace.hopper@example.com");

        // Act
        HttpResponseMessage sut = await factory.CreateClient()
            .GetAsync($"{CustomersRoute}/{seeded.PublicId.Value}", cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await sut.Content.ReadFromJsonAsync<CustomerResponse>(cancellationToken);
        body.Should().NotBeNull();
        body!.PublicId.Should().Be(seeded.PublicId);
        body.FirstName.Should().Be("Grace");
        body.LastName.Should().Be("Hopper");
        body.DisplayName.Should().Be("Grace Hopper");
        body.Email.Should().Be("grace.hopper@example.com");
        body.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Should_Return_NotFound_For_Unknown_PublicId()
    {
        var unknownPublicId = PublicIdFactory.ForCustomer().Value.Value;

        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .GetAsync($"{CustomersRoute}/{unknownPublicId}", CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_Return_BadRequest_For_Malformed_PublicId()
    {
        HttpResponseMessage sut = await ConfiguredFactory.CreateClient()
            .GetAsync($"{CustomersRoute}/not-a-valid-public-id", CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<CustomerAggregate> SeedCustomerAsync(
        WebApplicationFactory<Program> factory,
        string firstName,
        string lastName,
        string email)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CustomersContext>();

        var customer = CustomerAggregate.Create(
            firstName,
            lastName,
            Email.Create(email).Value,
            phone: null,
            createdAt: DateTimeOffset.UtcNow);

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        return customer;
    }
}
