using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ShowRoom.Modules.Customer.Persistence;


/// <summary>
/// Provides extension methods for configuring the database module.
/// </summary>
public static class DatabaseModule
{
    private const string ModuleConnectionStringName = "Customers";
    // Database-per-service: the Customer service owns its own database (Aspire resource).
    private const string AspireConnectionStringName = "showroom-customers";

    /// <summary>
    /// Adds the database module to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string? connectionString = configuration.GetConnectionString(AspireConnectionStringName)
            ?? configuration.GetConnectionString(ModuleConnectionStringName);

        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        //services.AddAuditingInterceptor();

        services.AddDbContext<CustomersContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, sqlOptions =>
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "customers"));

            if (env == Environments.Development)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            //options.AddInterceptors(provider.GetRequiredService<AuditingSaveChangesInterceptor>());
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
