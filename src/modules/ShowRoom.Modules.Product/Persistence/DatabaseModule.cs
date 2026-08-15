using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ShowRoom.Modules.Product.Persistence;

/// <summary>
/// Provides extension methods for configuring the Product database module. Uses plain
/// <c>AddDbContext</c> (not Aspire's <c>AddNpgsqlDbContext</c>) so integration tests can cleanly
/// repoint the context at an isolated container.
/// </summary>
public static class DatabaseModule
{
    // Single shared database, always provided at runtime under this name: Aspire (WithReference), an
    // environment variable, or the test WebApplicationFactory. The module is isolated by its own schema.
    private const string ConnectionStringName = "showroom";

    /// <summary>Adds the Product database context to the service collection.</summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string? connectionString = configuration.GetConnectionString(ConnectionStringName);

        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<ProductsContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, sqlOptions =>
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", ProductsContext.Schema));

            if (env == Environments.Development)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    /// <summary>Applies pending migrations for the Product module.</summary>
    public static IApplicationBuilder UseDatabase(this IApplicationBuilder builder)
    {
        builder.UseAutomaticMigrations();

        return builder;
    }
}
