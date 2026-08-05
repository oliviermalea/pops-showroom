using ShowRoom.BuildingBlocks.Domain.Abstractions;
using ShowRoom.BuildingBlocks.Domain.Primitives;
using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.Modules.Order.Domain;

/// <summary>
/// Order aggregate root. Carries a strongly-typed internal <see cref="OrderId"/> and the externally
/// exposed <see cref="AggregateRootWithPublicId{TId}.PublicId"/>. The owning customer and each product
/// are referenced by <see cref="PublicId"/> value only — there is no cross-module database link — which
/// is exactly the modulith boundary this POC demonstrates.
/// </summary>
public sealed class Order : AggregateRootWithPublicId<OrderId>, IAuditable
{
    public const string DefaultCurrency = "EUR";

    private readonly List<OrderLine> _lines = [];

    private Order(OrderId id)
        : base() => Id = id;

    private Order(
        OrderId id,
        PublicId publicId,
        PublicId customerPublicId,
        string currency,
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

    public string Currency { get; private set; } = DefaultCurrency;

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
            string.IsNullOrWhiteSpace(currency) ? DefaultCurrency : currency.Trim().ToUpperInvariant(),
            OrderStatus.Pending,
            createdAt,
            updatedAt: null);

        order._lines.AddRange(builtLines);

        return Result<Order>.Success(order);
    }

    /// <summary>Rehydrates an aggregate from already-persisted state (used by EF).</summary>
    public static Order Restore(
        OrderId id,
        PublicId publicId,
        PublicId customerPublicId,
        string currency,
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
