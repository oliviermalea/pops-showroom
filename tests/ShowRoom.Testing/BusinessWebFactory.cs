namespace ShowRoom.Testing;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Testcontainers.PostgreSql;
using Wolverine;

/// <summary>
/// Boots an API (identified by <typeparamref name="TEntryPoint"/>) against a dedicated PostgreSQL
/// container. Module test factories override <see cref="ConfigureModuleTestServices"/> to point their
/// DbContext at this container and <see cref="InitializeModuleTestServices"/> to migrate it. Generic
/// over the entry point so each service (Business API, Customer API, …) can reuse the same harness.
/// </summary>
public class BusinessWebFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>, IAsyncLifetime
    where TEntryPoint : class
{
    private readonly Logger _logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateLogger();

    protected readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("showroom")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.IntegrationTests.json", optional: true, reloadOnChange: true);

            // Applied AFTER the json (so overrides win) and as HOST configuration (so it is visible to
            // Program.cs when modules read connection strings / feature flags at build time). A factory
            // that needs a real broker or extra modules supplies its values here.
            var overrides = HostConfigurationOverrides();
            if (overrides.Count > 0)
            {
                configurationBuilder.AddInMemoryCollection(overrides);
            }
        });

        return base.CreateHost(builder);
    }

    /// <summary>
    /// Host-configuration overrides applied on top of appsettings.IntegrationTests.json. Empty by
    /// default; override to inject connection strings (e.g. a real broker) or feature flags.
    /// </summary>
    protected virtual IDictionary<string, string?> HostConfigurationOverrides()
        => new Dictionary<string, string?>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSerilog(_logger);
        });

        builder.ConfigureTestServices(services =>
        {
            // By default, stub the external (RabbitMQ) transports so integration hosts boot without a
            // broker. The dedicated end-to-end factory overrides StubExternalTransports to exercise a
            // real broker.
            if (StubExternalTransports)
            {
                services.DisableAllExternalWolverineTransports();
            }

            ConfigureModuleTestServices(services);

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            InitializeModuleTestServices(scope.ServiceProvider);
        });
    }

    /// <summary>
    /// When <c>true</c> (default), Wolverine's external transports are stubbed so no RabbitMQ broker is
    /// needed. Override to <c>false</c> in a factory that provides a real broker for end-to-end tests.
    /// </summary>
    protected virtual bool StubExternalTransports => true;

    protected virtual void ConfigureModuleTestServices(IServiceCollection services)
    {
    }

    protected virtual void InitializeModuleTestServices(IServiceProvider serviceProvider)
    {
    }

    public virtual async ValueTask InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgreSqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
