namespace ShowRoom.Modules.Customer.Contracts.Messaging;

/// <summary>
/// Well-known names for the Customer module's outbound message contract (AMQP). Kept in the contract
/// assembly so the publisher (routing) and every consumer agree on a single source of truth.
/// </summary>
public static class CustomerMessagingContract
{
    /// <summary>Queue carrying <see cref="CustomerRegisteredIntegrationEvent"/> notifications.</summary>
    public const string CustomerRegisteredQueue = "showroom.customers.customer-registered";
}
