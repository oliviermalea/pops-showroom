namespace ShowRoom.Modules.Customer.IntegrationTests.Features.ChangeCustomerEmail.Endpoints;

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.BuildingBlocks.Application;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Customer;
using ShowRoom.Modules.Customer.Domain;
using ShowRoom.Modules.Customer.Features.ChangeCustomerEmail;
using ShowRoom.Modules.Customer.Features.GetCustomerByPublicId;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.SharedKernel.Emails;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

public class ChangeCustomerEmailEndpointTests(CustomerBusinessWebFactory applicationInMemoryFactory)
    : IClassFixture<CustomerBusinessWebFactory>
{
    private static readonly string CustomersRoute = $"/api/v1/{CustomerModule.RouteSegment}";

    private WebApplicationFactory<Program> ConfiguredFactory => applicationInMemoryFactory;

    [Fact]
    public async Task Should_Change_Email_And_Return_NoContent()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var factory = ConfiguredFactory;
        var seeded = await SeedCustomerAsync(factory, $"{Guid.NewGuid():N}@example.com");
        var newEmail = $"{Guid.NewGuid():N}@example.com";

        // Act
        HttpResponseMessage patch = await factory.CreateClient().PatchAsJsonAsync(
            $"{CustomersRoute}/{seeded.PublicId.Value}/email",
            new ChangeCustomerEmailRequest(newEmail),
            cancellationToken);

        // Assert
        patch.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await factory.CreateClient().GetAsync($"{CustomersRoute}/{seeded.PublicId.Value}", cancellationToken);
        var body = await get.Content.ReadFromJsonAsync<CustomerResponse>(cancellationToken);
        body.Should().NotBeNull();
        body!.Email.Should().Be(newEmail);
    }

    [Fact]
    public async Task Should_Return_NotFound_For_Unknown_Customer()
    {
        var unknown = PublicIdFactory.ForCustomer().Value.Value;

        HttpResponseMessage sut = await ConfiguredFactory.CreateClient().PatchAsJsonAsync(
            $"{CustomersRoute}/{unknown}/email",
            new ChangeCustomerEmailRequest("someone@example.com"),
            CancellationToken.None);

        sut.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Email_Belongs_To_Another_Customer()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var factory = ConfiguredFactory;
        var takenEmail = $"{Guid.NewGuid():N}@example.com";
        await SeedCustomerAsync(factory, takenEmail);
        var target = await SeedCustomerAsync(factory, $"{Guid.NewGuid():N}@example.com");

        // Act — try to move target onto an address another customer already owns
        HttpResponseMessage sut = await factory.CreateClient().PatchAsJsonAsync(
            $"{CustomersRoute}/{target.PublicId.Value}/email",
            new ChangeCustomerEmailRequest(takenEmail),
            cancellationToken);

        // Assert
        sut.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Should_Return_BadRequest_For_Invalid_Email()
    {
        var cancellationToken = CancellationToken.None;
        var factory = ConfiguredFactory;
        var seeded = await SeedCustomerAsync(factory, $"{Guid.NewGuid():N}@example.com");

        HttpResponseMessage sut = await factory.CreateClient().PatchAsJsonAsync(
            $"{CustomersRoute}/{seeded.PublicId.Value}/email",
            new ChangeCustomerEmailRequest("not-an-email"),
            cancellationToken);

        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Changing_the_email_dispatches_the_domain_event_after_commit()
    {
        // Arrange — a probe handler registered alongside the real one, to observe the dispatch.
        var cancellationToken = CancellationToken.None;
        var recorder = new DomainEventRecorder();
        var factory = ConfiguredFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(recorder);
                services.AddScoped<IDomainEventHandler<CustomerEmailChanged>, RecordingCustomerEmailChangedHandler>();
            }));

        var seeded = await SeedCustomerAsync(factory, $"{Guid.NewGuid():N}@example.com");
        var newEmail = $"{Guid.NewGuid():N}@example.com";

        // Act
        HttpResponseMessage patch = await factory.CreateClient().PatchAsJsonAsync(
            $"{CustomersRoute}/{seeded.PublicId.Value}/email",
            new ChangeCustomerEmailRequest(newEmail),
            cancellationToken);

        // Assert — the interceptor dispatched CustomerEmailChanged to the handler after commit.
        patch.StatusCode.Should().Be(HttpStatusCode.NoContent);
        recorder.Events.Should().ContainSingle(e => e.PublicId == seeded.PublicId && e.NewEmail == newEmail);
    }

    private sealed class DomainEventRecorder
    {
        public List<CustomerEmailChanged> Events { get; } = [];
    }

    private sealed class RecordingCustomerEmailChangedHandler(DomainEventRecorder recorder)
        : IDomainEventHandler<CustomerEmailChanged>
    {
        public Task Handle(CustomerEmailChanged domainEvent, CancellationToken cancellationToken)
        {
            recorder.Events.Add(domainEvent);
            return Task.CompletedTask;
        }
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
