namespace ShowRoom.BuildingBlocks.Time;

/// <summary>Abstracts the current UTC time so it can be controlled in tests.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
