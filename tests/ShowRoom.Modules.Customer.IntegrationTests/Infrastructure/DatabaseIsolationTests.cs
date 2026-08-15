namespace ShowRoom.Modules.Customer.IntegrationTests.Infrastructure;

using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.Modules.Customer.Persistence;

/// <summary>
/// Safety net: proves the integration host talks to the throwaway Testcontainers PostgreSQL container —
/// NEVER a real (dev / Aspire / prod) database. Both the business <see cref="CustomersContext"/> and the
/// Wolverine message store bind to the SAME container connection string that the factory injects through
/// host configuration.
/// </summary>
public sealed class DatabaseIsolationTests(CustomerBusinessWebFactory factory)
    : IClassFixture<CustomerBusinessWebFactory>
{
    [Fact]
    public void The_business_DbContext_uses_the_testcontainers_database()
    {
        // Force the host to build, then inspect what the DbContext is actually pointed at.
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CustomersContext>();

        var actual = context.Database.GetConnectionString();

        actual.Should().Be(factory.ContainerConnectionString);
    }

    [Fact]
    public void The_wolverine_message_store_uses_the_same_testcontainers_database()
    {
        // The outbox writes envelopes through the DbContext connection, so the message store MUST resolve
        // the same "showroom" connection string — which is the container, not any real environment value.
        var configuration = factory.Services.GetRequiredService<IConfiguration>();

        var showroom = configuration.GetConnectionString("showroom");

        showroom.Should().Be(factory.ContainerConnectionString);
    }

    [Fact]
    public void The_connection_string_is_an_ephemeral_local_container()
    {
        // Testcontainers publishes PostgreSQL on a random localhost port — a real shared/prod database
        // would not look like this.
        var connectionString = factory.ContainerConnectionString;

        connectionString.Should().MatchRegex("Host=(127\\.0\\.0\\.1|localhost)");
        connectionString.Should().Contain("Database=showroom");
    }
}
