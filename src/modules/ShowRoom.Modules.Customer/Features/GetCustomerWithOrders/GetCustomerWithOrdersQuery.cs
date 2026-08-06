using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Customer.Features.GetCustomerWithOrders;

/// <summary>
/// Query: fetch a customer together with their order history. The orders are fetched from the Order
/// module over the message bus (AMQP request/reply), not via HTTP or a shared database.
/// </summary>
public sealed record GetCustomerWithOrdersQuery(PublicId PublicId);
