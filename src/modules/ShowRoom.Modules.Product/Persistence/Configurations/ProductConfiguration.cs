using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Product.Domain;
using ShowRoom.SharedKernel.Currencies;
using ProductAggregate = ShowRoom.Modules.Product.Domain.Product;

namespace ShowRoom.Modules.Product.Persistence.Configurations;

/// <summary>
/// EF Core mapping that bridges the <see cref="ProductAggregate"/> domain aggregate to storage.
/// Strongly-typed ids and value objects are persisted through value converters; there is no POCO.
/// </summary>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<ProductAggregate>
{
    public void Configure(EntityTypeBuilder<ProductAggregate> builder)
    {
        builder.ToTable("products");

        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id)
            .HasConversion(id => id.Value, value => ProductId.FromGuid(value))
            .ValueGeneratedNever();

        builder.Property(product => product.PublicId)
            .HasConversion(publicId => publicId.Value, value => PublicId.Parse(value))
            .HasMaxLength(36)
            .IsRequired();
        builder.HasIndex(product => product.PublicId).IsUnique();

        builder.Property(product => product.Name).HasMaxLength(300).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(2000);

        builder.Property(product => product.Price).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(product => product.Currency)
            .HasConversion(currency => currency.Code, code => Currency.FromName(code))
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(product => product.Status)
            .HasConversion(status => status.Value, value => ProductStatus.FromValue(value))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(product => product.CreatedAt).IsRequired();
        builder.Property(product => product.UpdatedAt);
    }
}
