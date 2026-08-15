namespace ShowRoom.Modules.Customer.IntegrationTests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.Testing;
using Wolverine.Runtime;

/// <summary>
/// Customer module test factory. Each test class gets its own throwaway PostgreSQL container
/// (<c>_postgreSqlContainer</c>, owned and disposed by the base factory) — fully isolated from every other
/// test and from any real database. The Customer module keeps a SINGLE <see cref="CustomersContext"/>: its
/// business tables live in the <c>customers</c> schema and Wolverine's transactional-outbox tables in the
/// <c>wolverine</c> schema of the SAME database (the outbox writes envelopes through the context's own
/// connection, which is what makes the write atomic). Binding <c>ConnectionStrings:showroom</c> to the
/// container through host configuration points both the DbContext and the Wolverine message store at that
/// one isolated container. External transports are stubbed by the base factory
/// (<c>DisableAllExternalWolverineTransports</c>), so the outbox persists and routes without a real broker;
/// Wolverine storage is cleared on teardown (<c>ClearAllWolverineStorageAsync</c>).
/// </summary>
public sealed class CustomerBusinessWebFactory : BusinessWebFactory<Program>
{
    private IHost? _host;

    /// <summary>The ephemeral Testcontainers PostgreSQL connection string this factory binds everything to.</summary>
    public string ContainerConnectionString => _postgreSqlContainer.GetConnectionString();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        _host = base.CreateHost(builder);
        return _host;
    }

    protected override IDictionary<string, string?> HostConfigurationOverrides() => new Dictionary<string, string?>
    {
        ["ConnectionStrings:showroom"] = _postgreSqlContainer.GetConnectionString(),
    };

    protected override void InitializeModuleTestServices(IServiceProvider serviceProvider)
        => serviceProvider.GetRequiredService<CustomersContext>()
            .Database
            .Migrate();

    public override async ValueTask DisposeAsync()
    {
        if (_host is not null)
        {
            // Clear Wolverine's inbox/outbox/dead-letter tables (recommended by the Wolverine testing guide).
            await _host.ClearAllWolverineStorageAsync();
        }

        await base.DisposeAsync();
    }
}
