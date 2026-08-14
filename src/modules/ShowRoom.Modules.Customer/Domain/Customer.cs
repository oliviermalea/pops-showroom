using ShowRoom.BuildingBlocks.Domain.Abstractions;
using ShowRoom.BuildingBlocks.Domain.Primitives;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Results;
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

    /// <summary>
    /// Changes the customer's email address. Intention-revealing behaviour (not a setter): it guards its
    /// invariant, is <b>idempotent</b> (an unchanged address is a no-op that raises nothing), stamps
    /// <see cref="UpdatedAt"/>, and raises <see cref="CustomerEmailChanged"/>. Cross-aggregate uniqueness
    /// (no other customer using the address) is an application concern enforced by the handler, not here.
    /// </summary>
    public Result ChangeEmail(Email newEmail, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(newEmail);

        if (Email == newEmail)
        {
            return Result.Success();
        }

        var previousEmail = Email;
        Email = newEmail;
        UpdatedAt = now;

        RaiseDomainEvent(new CustomerEmailChanged(
            Guid.CreateVersion7(),
            Id,
            PublicId,
            previousEmail.Value,
            newEmail.Value));

        return Result.Success();
    }

    /// <summary>
    /// Renames the customer. Guards the invariant (a customer always has a first and last name), is
    /// idempotent, stamps <see cref="UpdatedAt"/>, and raises <see cref="CustomerRenamed"/> on a real
    /// change.
    /// </summary>
    public Result Rename(string firstName, string lastName, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return CustomerErrors.NameRequired;
        }

        var newFirstName = firstName.Trim();
        var newLastName = lastName.Trim();

        if (FirstName == newFirstName && LastName == newLastName)
        {
            return Result.Success();
        }

        FirstName = newFirstName;
        LastName = newLastName;
        UpdatedAt = now;

        RaiseDomainEvent(new CustomerRenamed(Guid.CreateVersion7(), Id, PublicId, newFirstName, newLastName));

        return Result.Success();
    }

    /// <summary>
    /// Sets or clears the customer's phone number (pass <c>null</c> to remove it). Idempotent, stamps
    /// <see cref="UpdatedAt"/>, and raises <see cref="CustomerPhoneChanged"/> on a real change.
    /// </summary>
    public Result ChangePhone(PhoneNumber? phone, DateTimeOffset now)
    {
        if (Phone?.Value == phone?.Value)
        {
            return Result.Success();
        }

        var previousPhone = Phone;
        Phone = phone;
        UpdatedAt = now;

        RaiseDomainEvent(new CustomerPhoneChanged(
            Guid.CreateVersion7(),
            Id,
            PublicId,
            previousPhone?.Value,
            phone?.Value));

        return Result.Success();
    }

    /// <summary>
    /// Domain-owned rehydration factory: reconstitutes an EXISTING aggregate from already-validated,
    /// persisted state — reusing the stored identity, accepting the stored status/timestamps, and raising
    /// NO creation events (reloading is not re-creating). This is the sanctioned way to rebuild the
    /// aggregate outside <see cref="Create"/>, independent of any persistence mechanism. NOTE: EF Core
    /// materialises via the private constructor, so it does not call this — it is exercised by tests and
    /// is the seam for any non-EF rehydration (event replay, snapshot, another store).
    /// </summary>
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
