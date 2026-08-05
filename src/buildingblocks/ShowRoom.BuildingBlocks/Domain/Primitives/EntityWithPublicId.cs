using ShowRoom.BuildingBlocks.Domain.PublicIds;

namespace ShowRoom.BuildingBlocks.Domain.Primitives;

/// <summary>Entity that exposes a public identifier alongside its internal identifier.</summary>
public abstract class EntityWithPublicId : Entity
{
    protected EntityWithPublicId()
    {
    }

    public PublicId PublicId { get; protected set; } = null!;
}
