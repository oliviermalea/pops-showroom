namespace ShowRoom.Modules.Order.IntegrationTests.Features.OnCustomerRegistered;

using AwesomeAssertions;
using ShowRoom.Modules.Customer.Contracts.Messaging;
using Wolverine.Tracking;

/// <summary>
/// Proves the cross-service consumer and its retry/dead-letter policy against a real RabbitMQ broker and a
/// real PostgreSQL message store. A well-formed <c>CustomerRegistered</c> message is handled successfully;
/// a poison message (missing public id) is retried a few times and then moved to the Wolverine dead-letter
/// store — never retried forever. Backed by PostgreSQL + RabbitMQ Testcontainers.
/// </summary>
public sealed class CustomerRegisteredConsumerE2ETests(CustomerRegisteredConsumerE2EFactory factory)
    : IClassFixture<CustomerRegisteredConsumerE2EFactory>
{
    [Fact]
    public async Task Well_formed_event_is_handled_successfully()
    {
        // Arrange — start the host (listener + message store) and build a valid event.
        _ = factory.CreateClient();
        var @event = new CustomerRegisteredIntegrationEvent(
            $"cus_{Guid.NewGuid():N}",
            "Grace",
            "Hopper",
            $"grace.hopper.{Guid.NewGuid():N}@example.com",
            DateTimeOffset.UtcNow);

        // Act — publish onto the queue (through the real broker) and wait for it to be received/handled.
        var tracked = await factory.Services
            .TrackActivity()
            .Timeout(TimeSpan.FromSeconds(30))
            .WaitForMessageToBeReceivedAt<CustomerRegisteredIntegrationEvent>(factory.Host)
            .ExecuteAndWaitAsync(context => context.PublishAsync(@event));

        // Assert — the consumer handled it successfully.
        tracked.MessageSucceeded.SingleMessage<CustomerRegisteredIntegrationEvent>().Should().NotBeNull();
    }

    [Fact]
    public async Task Poison_event_is_retried_then_moved_to_the_dead_letter_store()
    {
        // Arrange — a poison message: no public id, so the handler cannot process it.
        _ = factory.CreateClient();
        var poison = new CustomerRegisteredIntegrationEvent(
            PublicId: string.Empty,
            FirstName: "X",
            LastName: "Y",
            Email: $"poison.{Guid.NewGuid():N}@example.com",
            RegisteredOn: DateTimeOffset.UtcNow);

        // Act — the handler throws on every attempt; the policy retries then dead-letters.
        var tracked = await factory.Services
            .TrackActivity()
            .Timeout(TimeSpan.FromSeconds(60))
            .DoNotAssertOnExceptionsDetected()
            .WaitForMessageToBeReceivedAt<CustomerRegisteredIntegrationEvent>(factory.Host)
            .ExecuteAndWaitAsync(context => context.PublishAsync(poison));

        // Assert — it ended up in the dead-letter store rather than being retried forever.
        tracked.MovedToErrorQueue.SingleMessage<CustomerRegisteredIntegrationEvent>().Should().NotBeNull();
    }
}
