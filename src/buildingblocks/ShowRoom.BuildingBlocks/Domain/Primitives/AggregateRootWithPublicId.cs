using ShowRoom.BuildingBlocks.Domain.PublicIds;
using ShowRoom.BuildingBlocks.Results;

namespace ShowRoom.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Aggregate root that exposes a public identifier (<see cref="PublicId"/>) alongside its internal,
/// strongly-typed identifier (declared by the derived type). Only the <see cref="PublicId"/> is
/// exposed over HTTP.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's internal identifier.</typeparam>
public abstract class AggregateRootWithPublicId<TId> : AggregateRoot
{
    protected AggregateRootWithPublicId()
    {
    }

    public PublicId PublicId { get; protected set; } = null!;

    protected static Result<PublicId> CreatePublicId(string prefix) => PublicId.Create(prefix);
}
