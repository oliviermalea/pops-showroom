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

/// <summary>
/// Boots the Business API against a dedicated PostgreSQL container. Module test factories override
/// <see cref="ConfigureModuleTestServices"/> to point their DbContext at this container and
/// <see cref="InitializeModuleTestServices"/> to migrate it.
/// </summary>
public class BusinessWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly Logger _logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateLogger();

    protected readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("showroom-business")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.IntegrationTests.json", optional: true, reloadOnChange: true);
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSerilog(_logger);
        });

        builder.ConfigureTestServices(services =>
        {
            ConfigureModuleTestServices(services);

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            InitializeModuleTestServices(scope.ServiceProvider);
        });
    }

    protected virtual void ConfigureModuleTestServices(IServiceCollection services)
    {
    }

    protected virtual void InitializeModuleTestServices(IServiceProvider serviceProvider)
    {
    }

    public async ValueTask InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgreSqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
