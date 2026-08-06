namespace ShowRoom.Modules.Order.Contracts.Messaging;

/// <summary>
/// Well-known names for the Order module's message contract (AMQP). Kept in the contract assembly so
/// the sender (routing) and the listener agree on a single source of truth.
/// </summary>
public static class OrderMessagingContract
{
    /// <summary>Queue carrying <see cref="GetOrdersForCustomer"/> request/reply traffic.</summary>
    public const string GetOrdersForCustomerQueue = "showroom.orders.get-orders-for-customer";
}
