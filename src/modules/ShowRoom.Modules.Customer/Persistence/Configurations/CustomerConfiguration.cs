using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Customer.Domain;
using ShowRoom.SharedKernel.Emails;
using ShowRoom.SharedKernel.PhoneNumbers;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Persistence.Configurations;

/// <summary>
/// EF Core mapping that bridges the <see cref="CustomerAggregate"/> domain aggregate to storage.
/// Strongly-typed ids and value objects are persisted through value converters; there is no POCO.
/// </summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<CustomerAggregate>
{
    public void Configure(EntityTypeBuilder<CustomerAggregate> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id)
            .HasConversion(id => id.Value, value => CustomerId.FromGuid(value))
            .ValueGeneratedNever();

        builder.Property(customer => customer.PublicId)
            .HasConversion(publicId => publicId.Value, value => PublicId.Parse(value))
            .HasMaxLength(36)
            .IsRequired();
        builder.HasIndex(customer => customer.PublicId).IsUnique();

        builder.Property(customer => customer.FirstName).HasMaxLength(200).IsRequired();
        builder.Property(customer => customer.LastName).HasMaxLength(200).IsRequired();

        builder.Property(customer => customer.Email)
            .HasConversion(email => email.Value, value => Email.Create(value).Value)
            .HasMaxLength(320)
            .IsRequired();
        builder.HasIndex(customer => customer.Email).IsUnique();

        builder.Property(customer => customer.Phone)
            .HasConversion(phone => phone!.Value, value => PhoneNumber.Create(value).Value)
            .HasMaxLength(40)
            .IsRequired(false);

        builder.Property(customer => customer.Status)
            .HasConversion(status => status.Value, value => CustomerStatus.FromValue(value))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(customer => customer.CreatedAt).IsRequired();
        builder.Property(customer => customer.UpdatedAt);

        // Computed, not persisted.
        builder.Ignore(customer => customer.DisplayName);
    }
}
