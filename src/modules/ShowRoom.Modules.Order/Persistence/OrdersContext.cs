using Microsoft.EntityFrameworkCore;
using ShowRoom.BuildingBlocks.Domain.Primitives;
using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Persistence;

/// <summary>
/// EF Core context owning the Order module schema. Maps the domain aggregate directly (no POCO);
/// the mapping lives entirely in the Persistence layer via <c>IEntityTypeConfiguration</c>.
/// </summary>
public sealed class OrdersContext : DbContext
{
    public const string Schema = "orders";

    public OrdersContext(DbContextOptions<OrdersContext> options)
        : base(options)
    {
    }

    public DbSet<OrderAggregate> Orders => Set<OrderAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(OrdersContext).Assembly,
            type => type.Namespace?.StartsWith("ShowRoom.Modules.Order.Persistence", StringComparison.Ordinal) == true);

        // Domain/integration events are behavioural, never persisted.
        modelBuilder.Ignore<DomainEvent>();

        base.OnModelCreating(modelBuilder);
    }
}
