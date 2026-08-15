namespace ShowRoom.Modules.Order.IntegrationTests.Features.OnCustomerRegistered;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShowRoom.Modules.Customer.Contracts.Messaging;
using ShowRoom.Testing;
using Testcontainers.RabbitMq;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.Runtime;

/// <summary>
/// End-to-end factory for the cross-service consumer: boots the Business host against a real PostgreSQL
/// message store (durable inbox + dead-letter, schema <c>wolverine_business</c>) AND a real RabbitMQ broker
/// (external transports NOT stubbed), so the whole path — a CustomerRegistered message arriving on the
/// queue, being handled or retried-then-dead-lettered — is exercised. A test-only publish route lets the
/// test inject the message onto the queue the production host only listens on.
/// </summary>
public sealed class CustomerRegisteredConsumerE2EFactory : BusinessWebFactory<Program>
{
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder().WithImage("rabbitmq:3.13").Build();
    private IHost? _host;

    protected override bool StubExternalTransports => false;

    /// <summary>The running host — needed to wait for a message to be received after the broker round-trip.</summary>
    public IHost Host => _host ?? throw new InvalidOperationException("The host has not been created yet.");

    protected override IHost CreateHost(IHostBuilder builder)
    {
        _host = base.CreateHost(builder);
        return _host;
    }

    protected override IDictionary<string, string?> HostConfigurationOverrides() => new Dictionary<string, string?>
    {
        ["ConnectionStrings:showroom"] = _postgreSqlContainer.GetConnectionString(),
        ["ConnectionStrings:messaging"] = _rabbitMq.GetConnectionString(),
        ["Messaging:UsePersistentMessageStore"] = "true",
        ["Messaging:MessageStoreSchema"] = "wolverine_business",
    };

    protected override void ConfigureModuleTestServices(IServiceCollection services)
        // Test-only publish route so the test can inject CustomerRegistered onto the queue the Business
        // host only listens on in production (the Customer service is the real producer). Wolverine applies
        // IWolverineExtension services from the container at bootstrap.
        => services.AddSingleton<IWolverineExtension, PublishCustomerRegisteredExtension>();

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await _rabbitMq.StartAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.ClearAllWolverineStorageAsync();
        }

        await _rabbitMq.DisposeAsync();
        await base.DisposeAsync();
    }
}

internal sealed class PublishCustomerRegisteredExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options)
        => options.PublishMessage<CustomerRegisteredIntegrationEvent>()
            .ToRabbitQueue(CustomerMessagingContract.CustomerRegisteredQueue);
}
