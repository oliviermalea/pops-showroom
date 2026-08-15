namespace ShowRoom.Modules.Customer.IntegrationTests.Features.CreateCustomer.Outbox;

using System.Net.Http.Json;
using AwesomeAssertions;
using ShowRoom.Modules.Customer;
using ShowRoom.Modules.Customer.Contracts.Messaging;
using ShowRoom.Modules.Customer.Features.CreateCustomer;
using Wolverine;
using Wolverine.Tracking;

/// <summary>
/// Proves the transactional outbox actually PUBLISHES <see cref="CustomerRegisteredIntegrationEvent"/> when
/// a customer is created — without a real broker. External transports are stubbed
/// (<c>DisableAllExternalWolverineTransports</c>), and Wolverine's message-tracking captures every envelope
/// the host sends during the HTTP call, so we can assert the event was emitted from the single-DbContext
/// outbox (persisted atomically with the customer insert, then flushed by the durable sending agent).
/// </summary>
public sealed class CreateCustomerOutboxTests(CustomerBusinessWebFactory factory)
    : IClassFixture<CustomerBusinessWebFactory>
{
    private static readonly string CustomersRoute = $"/api/v1/{CustomerModule.RouteSegment}";

    [Fact]
    public async Task Creating_a_customer_publishes_the_CustomerRegistered_event_through_the_outbox()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var email = $"grace.hopper.{Guid.NewGuid():N}@example.com";
        var command = new CreateCustomerCommand
        {
            FirstName = "Grace",
            LastName = "Hopper",
            Email = email,
            Phone = "+33123456789",
        };
        var client = factory.CreateClient();

        // Act — run the HTTP create inside a tracked session so the outbox send is observed
        var tracked = await factory.Services
            .TrackActivity()
            .Timeout(TimeSpan.FromSeconds(30))
            .DoNotAssertOnExceptionsDetected()
            .ExecuteAndWaitAsync((Func<IMessageContext, Task>)(async _ =>
            {
                var response = await client.PostAsJsonAsync(CustomersRoute, command, cancellationToken);
                response.EnsureSuccessStatusCode();
            }));

        // Assert — the integration event was published through the outbox
        var published = tracked.Sent.SingleMessage<CustomerRegisteredIntegrationEvent>();
        published.Email.Should().Be(email);
        published.FirstName.Should().Be("Grace");
        published.LastName.Should().Be("Hopper");
        published.PublicId.Should().StartWith("cus_");
    }
}
