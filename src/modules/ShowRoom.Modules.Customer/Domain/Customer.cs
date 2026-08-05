using ShowRoom.BuildingBlocks.Domain.Abstractions;
using ShowRoom.BuildingBlocks.Domain.Primitives;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.SharedKernel.Emails;
using ShowRoom.SharedKernel.PhoneNumbers;

namespace ShowRoom.Modules.Customer.Domain;

/// <summary>
/// Customer aggregate root. Carries a strongly-typed internal <see cref="CustomerId"/> and the
/// externally exposed <see cref="AggregateRootWithPublicId{TId}.PublicId"/> (from the base);
/// primitive identifiers are never used in the domain.
/// </summary>
public sealed class Customer : AggregateRootWithPublicId<CustomerId>, IAuditable
{
    private Customer(CustomerId id)
        : base() => Id = id;

    private Customer(
        CustomerId id,
        PublicId publicId,
        string firstName,
        string lastName,
        Email email,
        PhoneNumber? phone,
        CustomerStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
        : this(id)
    {
        PublicId = publicId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public CustomerId Id { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string DisplayName => $"{FirstName} {LastName}".Trim();

    public Email Email { get; private set; } = null!;

    public PhoneNumber? Phone { get; private set; }

    public CustomerStatus Status { get; private set; } = CustomerStatus.Active;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Creates a brand new active customer with fresh internal and public identifiers.</summary>
    public static Customer Create(
        string firstName,
        string lastName,
        Email email,
        PhoneNumber? phone,
        DateTimeOffset createdAt)
        => new(
            CustomerId.FromGuid(Guid.CreateVersion7()),
            PublicIdFactory.ForCustomer().Value,
            firstName,
            lastName,
            email,
            phone,
            CustomerStatus.Active,
            createdAt,
            updatedAt: null);

    /// <summary>Rehydrates an aggregate from already-persisted state (used by the repository/EF).</summary>
    public static Customer Restore(
        CustomerId id,
        PublicId publicId,
        string firstName,
        string lastName,
        Email email,
        PhoneNumber? phone,
        CustomerStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
        => new(id, publicId, firstName, lastName, email, phone, status, createdAt, updatedAt);
}
