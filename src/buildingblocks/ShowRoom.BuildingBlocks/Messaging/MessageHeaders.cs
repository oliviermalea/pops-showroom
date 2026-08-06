namespace ShowRoom.BuildingBlocks.Messaging;

public static class MessageHeaders
{
    public const string ModuleName = "x-module-name";
    public const string FeatureName = "x-feature-name";
    public const string CorrelationId = "x-correlation-id";
    public const string CausationId = "x-causation-id";
    public const string MessageId = "x-message-id";
    public const string MessageType = "x-message-type";
    public const string EventType = "x-event-type";
    public const string TenantId = "x-tenant-id";
    public const string TraceId = "x-trace-id";
    public const string UserId = "x-user-id";
}