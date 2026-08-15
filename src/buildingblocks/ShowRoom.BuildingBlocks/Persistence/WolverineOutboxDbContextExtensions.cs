using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShowRoom.BuildingBlocks.Messaging;
using Wolverine.EntityFrameworkCore;

namespace ShowRoom.BuildingBlocks.Persistence;

/// <summary>
/// Registers a module <see cref="DbContext"/> either with Wolverine's transactional-outbox integration
/// (when <c>Messaging:UseTransactionalOutbox</c> is on) or as a plain EF Core context otherwise. When the
/// outbox is enabled the context is registered via <c>AddDbContextWithWolverineIntegration</c>, which also
/// exposes an <c>IDbContextOutbox&lt;TContext&gt;</c> so a handler can publish IntegrationEvents in the same
/// database transaction as the business change — keeping a SINGLE DbContext (no second context) while the
/// Wolverine tables live in the message-store schema. That schema is provisioned by the message store (see
/// <c>ConfigureShowRoomMessaging</c>) and is NOT part of the EF model, so the module's migrations are
/// unaffected.
/// </summary>
/// <remarks>
/// The outbox writes envelopes through the context's own connection, so the message store and the module
/// database MUST be the same physical database (both resolve from <c>Messaging:MessageStoreConnectionName</c>,
/// default <c>showroom</c>). Enabling the outbox therefore requires the host to configure the Postgres
/// message store; without it Wolverine fails fast at startup.
/// </remarks>
public static class WolverineOutboxDbContextExtensions
{
    public static IServiceCollection AddDbContextWithOptionalOutbox<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IServiceProvider, DbContextOptionsBuilder> configureOptions)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var messaging = MessagingOptions.FromConfiguration(configuration);

        if (messaging is { Enabled: true, UseTransactionalOutbox: true })
        {
            services.AddDbContextWithWolverineIntegration<TContext>(configureOptions, messaging.MessageStoreSchema);
        }
        else
        {
            services.AddDbContext<TContext>(configureOptions);
        }

        return services;
    }
}
