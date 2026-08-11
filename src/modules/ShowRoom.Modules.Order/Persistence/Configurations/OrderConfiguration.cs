using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Order.Domain;
using ShowRoom.SharedKernel.Currencies;
using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Persistence.Configurations;

/// <summary>
/// EF Core mapping that bridges the <see cref="OrderAggregate"/> domain aggregate to storage.
/// Strongly-typed ids and value objects are persisted through value converters; order lines are an
/// owned collection (part of the aggregate, never queried independently). There is no POCO.
/// </summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<OrderAggregate>
{
    public void Configure(EntityTypeBuilder<OrderAggregate> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id)
            .HasConversion(id => id.Value, value => OrderId.FromGuid(value))
            .ValueGeneratedNever();

        builder.Property(order => order.PublicId)
            .HasConversion(publicId => publicId.Value, value => PublicId.Parse(value))
            .HasMaxLength(36)
            .IsRequired();
        builder.HasIndex(order => order.PublicId).IsUnique();

        // Cross-module reference by value only (no foreign key into the Customer module).
        builder.Property(order => order.CustomerPublicId)
            .HasConversion(publicId => publicId.Value, value => PublicId.Parse(value))
            .HasMaxLength(36)
            .IsRequired();
        builder.HasIndex(order => order.CustomerPublicId);

        builder.Property(order => order.Currency)
            .HasConversion(currency => currency.Code, code => Currency.FromName(code))
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion(status => status.Value, value => OrderStatus.FromValue(value))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(order => order.CreatedAt).IsRequired();
        builder.Property(order => order.UpdatedAt);

        // Computed, not persisted.
        builder.Ignore(order => order.TotalAmount);

        builder.OwnsMany(order => order.Lines, lines =>
        {
            lines.ToTable("order_lines");
            lines.WithOwner().HasForeignKey("OrderId");

            lines.HasKey(line => line.Id);
            lines.Property(line => line.Id)
                .HasConversion(id => id.Value, value => OrderLineId.FromGuid(value))
                .ValueGeneratedNever();

            lines.Property(line => line.ProductPublicId)
                .HasConversion(publicId => publicId.Value, value => PublicId.Parse(value))
                .HasMaxLength(36)
                .IsRequired();

            lines.Property(line => line.ProductName).HasMaxLength(300).IsRequired();
            lines.Property(line => line.Quantity).IsRequired();
            lines.Property(line => line.UnitPrice).HasColumnType("numeric(18,2)").IsRequired();

            lines.Ignore(line => line.LineTotal);
        });

        // The Lines collection is exposed read-only over a backing field.
        builder.Navigation(order => order.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
