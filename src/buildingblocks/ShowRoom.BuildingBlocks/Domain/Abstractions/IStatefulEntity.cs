using Ardalis.SmartEnum;

namespace ShowRoom.BuildingBlocks.Domain.Abstractions;

/// <summary>An entity that carries a low-cardinality status modelled as a SmartEnum.</summary>
public interface IStatefulEntity<TStatus>
    where TStatus : SmartEnum<TStatus>
{
    TStatus Status { get; }
}
