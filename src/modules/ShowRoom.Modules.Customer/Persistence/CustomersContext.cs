using Microsoft.EntityFrameworkCore;
using ShowRoom.BuildingBlocks.Domain.Primitives;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Persistence;

/// <summary>
/// EF Core context owning the Customer module schema. Maps the domain aggregate directly (no POCO);
/// the mapping lives entirely in the Persistence layer via <c>IEntityTypeConfiguration</c>.
/// </summary>
public sealed class CustomersContext : DbContext
{
    public const string Schema = "customers";

    public CustomersContext(DbContextOptions<CustomersContext> options)
        : base(options)
    {
    }

    public DbSet<CustomerAggregate> Customers => Set<CustomerAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CustomersContext).Assembly,
            type => type.Namespace?.StartsWith("ShowRoom.Modules.Customer.Persistence", StringComparison.Ordinal) == true);

        // Domain/integration events are behavioural, never persisted.
        modelBuilder.Ignore<DomainEvent>().Ignore<IntegrationEvent>();

        base.OnModelCreating(modelBuilder);
    }
}
