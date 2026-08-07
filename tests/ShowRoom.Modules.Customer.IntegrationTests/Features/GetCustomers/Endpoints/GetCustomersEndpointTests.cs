namespace ShowRoom.Modules.Customer.IntegrationTests.Features.GetCustomers.Endpoints;

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.Modules.Customer;
using ShowRoom.Modules.Customer.Features.GetCustomers;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.SharedKernel.Emails;
using ShowRoom.Testing.Configuration;
using ShowRoom.Testing.Database;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

public class GetCustomersEndpointTests(
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
    public async Task Should_Paginate_Results()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var factory = ConfiguredFactory;
        await SeedCustomerAsync(factory, "Alan", "Turing", $"{Guid.NewGuid():N}@example.com");
        await SeedCustomerAsync(factory, "Grace", "Hopper", $"{Guid.NewGuid():N}@example.com");
        await SeedCustomerAsync(factory, "Ada", "Lovelace", $"{Guid.NewGuid():N}@example.com");

        // Act
        HttpResponseMessage sut = await factory.CreateClient()
            .GetAsync($"{CustomersRoute}?page=1&pageSize=2", cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await sut.Content.ReadFromJsonAsync<GetCustomersResponse>(cancellationToken);
        body.Should().NotBeNull();
        body!.PageSize.Should().Be(2);
        body.Customers.Should().HaveCount(2);
        body.TotalItems.Should().BeGreaterThanOrEqualTo(3);
        body.Page.Should().Be(1);
    }

    [Fact]
    public async Task Should_Filter_By_Name_Search()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var factory = ConfiguredFactory;
        var uniqueLastName = $"Zzz{Guid.NewGuid():N}";
        await SeedCustomerAsync(factory, "Katherine", uniqueLastName, $"{Guid.NewGuid():N}@example.com");
        await SeedCustomerAsync(factory, "Dorothy", "Vaughan", $"{Guid.NewGuid():N}@example.com");

        // Act
        HttpResponseMessage sut = await factory.CreateClient()
            .GetAsync($"{CustomersRoute}?search={uniqueLastName}", cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await sut.Content.ReadFromJsonAsync<GetCustomersResponse>(cancellationToken);
        body.Should().NotBeNull();
        body!.Customers.Should().ContainSingle();
        body.Customers.Should().OnlyContain(customer => customer.LastName == uniqueLastName);
        body.TotalItems.Should().Be(1);
    }

    private static async Task SeedCustomerAsync(
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
    }
}
