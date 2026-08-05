using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.Modules.Order.Domain;
using OrderAggregate = ShowRoom.Modules.Order.Domain.Order;

namespace ShowRoom.Modules.Order.Features.CreateOrder;

/// <summary>
/// Maps the create-order command into domain line drafts, and a freshly created
/// <see cref="OrderAggregate"/> to its public identifier.
/// </summary>
internal static class CreateOrderAssembler
{
    public static IReadOnlyCollection<OrderLineDraft> ToDrafts(CreateOrderCommand command)
        => command.Lines
            .Select(line => new OrderLineDraft(
                PublicId.Parse(line.ProductPublicId),
                line.ProductName,
                line.Quantity,
                line.UnitPrice))
            .ToList();

    public static PublicId From(OrderAggregate order) => order.PublicId;
}
