using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShowRoom.BuildingBlocks.Persistence;

namespace ShowRoom.Modules.Customer.Persistence;


/// <summary>
/// Provides extension methods for configuring the database module.
/// </summary>
public static class DatabaseModule
{
    // Single shared database, always provided at runtime under this name: Aspire (WithReference), an
    // environment variable, or the test WebApplicationFactory. The module is isolated by its own schema.
    private const string ConnectionStringName = "showroom";

    /// <summary>
    /// Adds the database module to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string? connectionString = configuration.GetConnectionString(ConnectionStringName);

        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Domain events are dispatched by an EF interceptor after commit — the DbContext stays free of
        // any event logic (see DomainEventDispatchInterceptor).
        services.AddDomainEventDispatch();

        // Single DbContext. When Messaging:UseTransactionalOutbox is on it is registered with Wolverine's
        // outbox integration (exposing IDbContextOutbox<CustomersContext> for atomic IntegrationEvent
        // delivery); otherwise it is a plain DbContext. The Wolverine tables live in the dedicated
        // "wolverine" schema of this same database (provisioned by the message store, outside migrations).
        services.AddDbContextWithOptionalOutbox<CustomersContext>(configuration, (provider, options) =>
        {
            options.UseNpgsql(connectionString, sqlOptions =>
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "customers"));

            if (env == Environments.Development)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            options.AddInterceptors(provider.GetRequiredService<DomainEventDispatchInterceptor>());
        });

        return services;
    }

    /// <summary>
    /// Uses the database module.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The updated application builder.</returns>
    public static IApplicationBuilder UseDatabase(this IApplicationBuilder builder)
    {
        builder.UseAutomaticMigrations();

        return builder;
    }
}
