using ShowRoom.BuildingBlocks.Domain.Abstractions;
using ShowRoom.BuildingBlocks.Domain.Primitives;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.SharedKernel.Currencies;

namespace ShowRoom.Modules.Product.Domain;

/// <summary>
/// Product aggregate root. Carries a strongly-typed internal <see cref="ProductId"/> and the
/// externally exposed <see cref="AggregateRootWithPublicId{TId}.PublicId"/>; primitive identifiers
/// are never used in the domain. Referenced by other modules (e.g. Order) by <see cref="PublicId"/>
/// value only.
/// </summary>
public sealed class Product : AggregateRootWithPublicId<ProductId>, IAuditable
{
    private Product(ProductId id)
        : base() => Id = id;

    private Product(
        ProductId id,
        PublicId publicId,
        string name,
        string? description,
        decimal price,
        Currency currency,
        ProductStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
        : this(id)
    {
        PublicId = publicId;
        Name = name;
        Description = description;
        Price = price;
        Currency = currency;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public ProductId Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public Currency Currency { get; private set; } = Currency.Default;

    public ProductStatus Status { get; private set; } = ProductStatus.Available;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a brand new available product. Validates its input and returns an error result rather
    /// than throwing when the input is invalid.
    /// </summary>
    public static Result<Product> Create(
        string name,
        string? description,
        decimal price,
        string? currency,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ProductErrors.NameRequired;
        }

        if (price < 0)
        {
            return ProductErrors.InvalidPrice;
        }

        var currencyResult = Currency.FromCodeOrDefault(currency);
        if (currencyResult.IsFailure)
        {
            return Result<Product>.Fail(currencyResult.Errors);
        }

        return Result<Product>.Success(new Product(
            ProductId.FromGuid(Guid.CreateVersion7()),
            PublicIdFactory.ForProduct().Value,
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            price,
            currencyResult.Value,
            ProductStatus.Available,
            createdAt,
            updatedAt: null));
    }

    /// <summary>
    /// Domain-owned rehydration factory: reconstitutes an EXISTING aggregate from already-validated,
    /// persisted state — reusing the stored identity, accepting the stored status/timestamps, and raising
    /// NO creation events (reloading is not re-creating). This is the sanctioned way to rebuild the
    /// aggregate outside <see cref="Create"/>, independent of any persistence mechanism. NOTE: EF Core
    /// materialises via the private constructor, so it does not call this — it is exercised by tests and
    /// is the seam for any non-EF rehydration (event replay, snapshot, another store).
    /// </summary>
    public static Product Restore(
        ProductId id,
        PublicId publicId,
        string name,
        string? description,
        decimal price,
        Currency currency,
        ProductStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
        => new(id, publicId, name, description, price, currency, status, createdAt, updatedAt);
}
