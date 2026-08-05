using Microsoft.EntityFrameworkCore;
using ShowRoom.BuildingBlocks.Domain.Primitives;
using ProductAggregate = ShowRoom.Modules.Product.Domain.Product;

namespace ShowRoom.Modules.Product.Persistence;

/// <summary>
/// EF Core context owning the Product module schema. Maps the domain aggregate directly (no POCO);
/// the mapping lives entirely in the Persistence layer via <c>IEntityTypeConfiguration</c>.
/// </summary>
public sealed class ProductsContext : DbContext
{
    public const string Schema = "products";

    public ProductsContext(DbContextOptions<ProductsContext> options)
        : base(options)
    {
    }

    public DbSet<ProductAggregate> Products => Set<ProductAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ProductsContext).Assembly,
            type => type.Namespace?.StartsWith("ShowRoom.Modules.Product.Persistence", StringComparison.Ordinal) == true);

        // Domain/integration events are behavioural, never persisted.
        modelBuilder.Ignore<DomainEvent>().Ignore<IntegrationEvent>();

        base.OnModelCreating(modelBuilder);
    }
}
