namespace ShowRoom.Modules.Customer.IntegrationTests.Features.UpdateCustomerProfile.Endpoints;

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Customer;
using ShowRoom.Modules.Customer.Features.GetCustomerByPublicId;
using ShowRoom.Modules.Customer.Features.UpdateCustomerProfile;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.SharedKernel.Emails;
using ShowRoom.Testing.Configuration;
using ShowRoom.Testing.Database;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

public class UpdateCustomerProfileEndpointTests(
    CustomerBusinessWebFactory applicationInMemoryFactory,
    DatabaseContainer database)
    : IClassFixture<CustomerBusinessWebFactory>,
      IClassFixture<DatabaseContainer>
{
    private static readonly string CustomersRoute = $"/api/v1/{CustomerModule.RouteSegment}";

    private WebApplicationFactory<Program> ConfiguredFactory =>
        applicationInMemoryFactory
            .WithContainerDatabaseConfigured(new CustomerDatabaseConfiguration(database.ConnectionString!));

    [Fact]
    public async Task Should_Update_All_Fields_And_Return_NoContent()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var factory = ConfiguredFactory;
        var seeded = await SeedCustomerAsync(factory, $"{Guid.NewGuid():N}@example.com");
        var newEmail = $"{Guid.NewGuid():N}@example.com";

        // Act
        HttpResponseMessage put = await factory.CreateClient().PutAsJsonAsync(
            $"{CustomersRoute}/{seeded.PublicId.Value}",
            new UpdateCustomerProfileRequest("Ada", "Lovelace", newEmail, "+33123456789"),
            cancellationToken);

        // Assert
        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await factory.CreateClient().GetAsync($"{CustomersRoute}/{seeded.PublicId.Value}", cancellationToken);
        var body = await get.Content.ReadFromJsonAsync<CustomerResponse>(cancellationToken);
        body.Should().NotBeNull();
        body!.FirstName.Should().Be("Ada");
        body.LastName.Should().Be("Lovelace");
        body.Email.Should().Be(newEmail);
        body.Phone.Should().Be("+33123456789");
    }

    [Fact]
    public async Task Should_Return_NotFound_For_Unknown_Customer()
    {
        var unknown = PublicIdFactory.ForCustomer().Value.Value;

        HttpResponseMessage sut = await ConfiguredFactory.CreateClient().PutAsJsonAsync(
            $"{CustomersRoute}/{unknown}",
            new UpdateCustomerProfileRequest("Ada", "Lovelace", "ada@example.com", null),
            CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Email_Belongs_To_Another_Customer()
    {
        var cancellationToken = CancellationToken.None;
        var factory = ConfiguredFactory;
        var takenEmail = $"{Guid.NewGuid():N}@example.com";
        await SeedCustomerAsync(factory, takenEmail);
        var target = await SeedCustomerAsync(factory, $"{Guid.NewGuid():N}@example.com");

        HttpResponseMessage sut = await factory.CreateClient().PutAsJsonAsync(
            $"{CustomersRoute}/{target.PublicId.Value}",
            new UpdateCustomerProfileRequest("Ada", "Lovelace", takenEmail, null),
            cancellationToken);

        sut.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Should_Return_BadRequest_For_Invalid_Phone()
    {
        var cancellationToken = CancellationToken.None;
        var factory = ConfiguredFactory;
        var seeded = await SeedCustomerAsync(factory, $"{Guid.NewGuid():N}@example.com");

        HttpResponseMessage sut = await factory.CreateClient().PutAsJsonAsync(
            $"{CustomersRoute}/{seeded.PublicId.Value}",
            new UpdateCustomerProfileRequest("Ada", "Lovelace", $"{Guid.NewGuid():N}@example.com", "not-a-french-number"),
            cancellationToken);

        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<CustomerAggregate> SeedCustomerAsync(
        WebApplicationFactory<Program> factory,
        string email)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CustomersContext>();

        var customer = CustomerAggregate.Create(
            "Grace", "Hopper", Email.Create(email).Value, phone: null, createdAt: DateTimeOffset.UtcNow);

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        return customer;
    }
}
