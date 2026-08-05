using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.Modules.Customer.Features.GetCustomerByPublicId;

/// <summary>Query: fetch the detailed view of a customer by its public identifier.</summary>
public sealed record GetCustomerByPublicIdQuery(PublicId PublicId);
