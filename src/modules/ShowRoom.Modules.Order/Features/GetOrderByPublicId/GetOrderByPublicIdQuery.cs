using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Order.Features.GetOrderByPublicId;

/// <summary>Query: fetch the detailed view of an order (with its lines) by its public identifier.</summary>
public sealed record GetOrderByPublicIdQuery(PublicId PublicId);
