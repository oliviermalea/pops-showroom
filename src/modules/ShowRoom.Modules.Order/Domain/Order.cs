using ShowRoom.BuildingBlocks.Domain.Abstractions;
using ShowRoom.BuildingBlocks.Domain.Primitives;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Results;
using ShowRoom.SharedKernel.Currencies;

namespace ShowRoom.Modules.Order.Domain;

/// <summary>
/// Order aggregate root. Carries a strongly-typed internal <see cref="OrderId"/> and the externally
/// exposed <see cref="AggregateRootWithPublicId{TId}.PublicId"/>. The owning customer and each product
/// are referenced by <see cref="PublicId"/> value only — there is no cross-module database link — which
/// is exactly the modulith boundary this POC demonstrates.
/// </summary>
public sealed class Order : AggregateRootWithPublicId<OrderId>, IAuditable
{
    private readonly List<OrderLine> _lines = [];

    private Order(OrderId id)
        : base() => Id = id;

    private Order(
        OrderId id,
        PublicId publicId,
        PublicId customerPublicId,
        Currency currency,
        OrderStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
        : this(id)
    {
        PublicId = publicId;
        CustomerPublicId = customerPublicId;
        Currency = currency;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public OrderId Id { get; private set; }

    public PublicId CustomerPublicId { get; private set; } = null!;

    public Currency Currency { get; private set; } = Currency.Default;

    public OrderStatus Status { get; private set; } = OrderStatus.Pending;

    public IReadOnlyCollection<OrderLine> Lines => _lines;

    public decimal TotalAmount => _lines.Sum(line => line.LineTotal);

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a brand new pending order for a customer with at least one line. Validates its input
    /// and returns an error result rather than throwing when the input is invalid.
    /// </summary>
    public static Result<Order> Create(
        PublicId customerPublicId,
        string? currency,
        IReadOnlyCollection<OrderLineDraft> lines,
        DateTimeOffset createdAt)
    {
        if (customerPublicId is null)
        {
            return OrderErrors.CustomerRequired;
        }

        if (lines is null || lines.Count == 0)
        {
            return OrderErrors.NoLines;
        }

        var currencyResult = Currency.FromCodeOrDefault(currency);
        if (currencyResult.IsFailure)
        {
            return Result<Order>.Fail(currencyResult.Errors);
        }

        var builtLines = new List<OrderLine>(lines.Count);
        foreach (var draft in lines)
        {
            var lineResult = OrderLine.Create(
                draft.ProductPublicId,
                draft.ProductName,
                draft.Quantity,
                draft.UnitPrice);

            if (lineResult.IsFailure)
            {
                return Result<Order>.Fail(lineResult.Errors);
            }

            builtLines.Add(lineResult.Value);
        }

        var order = new Order(
            OrderId.FromGuid(Guid.CreateVersion7()),
            PublicIdFactory.ForOrder().Value,
            customerPublicId,
            currencyResult.Value,
            OrderStatus.Pending,
            createdAt,
            updatedAt: null);

        order._lines.AddRange(builtLines);

        return Result<Order>.Success(order);
    }

    /// <summary>
    /// Domain-owned rehydration factory: reconstitutes an EXISTING aggregate from already-validated,
    /// persisted state — reusing the stored identity, accepting the stored status/timestamps, and raising
    /// NO creation events (reloading is not re-creating). This is the sanctioned way to rebuild the
    /// aggregate outside <see cref="Create"/>, independent of any persistence mechanism. NOTE: EF Core
    /// materialises via the private constructor, so it does not call this — it is exercised by tests and
    /// is the seam for any non-EF rehydration (import/migration, snapshot/cache, event replay). Feed it
    /// only trusted, already-valid state (it deliberately skips creation invariants).
    /// </summary>
    public static Order Restore(
        OrderId id,
        PublicId publicId,
        PublicId customerPublicId,
        Currency currency,
        OrderStatus status,
        IEnumerable<OrderLine> lines,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
    {
        var order = new Order(id, publicId, customerPublicId, currency, status, createdAt, updatedAt);
        order._lines.AddRange(lines);

        return order;
    }
}
