using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ShowRoom.Modules.Order.Persistence;

/// <summary>
/// Provides extension methods for configuring the Order database module. Uses plain
/// <c>AddDbContext</c> (not Aspire's <c>AddNpgsqlDbContext</c>) so integration tests can cleanly
/// repoint the context at an isolated container.
/// </summary>
public static class DatabaseModule
{
    private const string ModuleConnectionStringName = "Orders";
    private const string AspireConnectionStringName = "showroom-business";

    /// <summary>Adds the Order database context to the service collection.</summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string? connectionString = configuration.GetConnectionString(AspireConnectionStringName)
            ?? configuration.GetConnectionString(ModuleConnectionStringName);

        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<OrdersContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, sqlOptions =>
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", OrdersContext.Schema));

            if (env == Environments.Development)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    /// <summary>Applies pending migrations for the Order module.</summary>
    public static IApplicationBuilder UseDatabase(this IApplicationBuilder builder)
    {
        builder.UseAutomaticMigrations();

        return builder;
    }
}
