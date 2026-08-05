namespace ShowRoom.BuildingBlocks.Time;

public sealed class DateTimeProvider(TimeProvider timeProvider) : IDateTimeProvider
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
