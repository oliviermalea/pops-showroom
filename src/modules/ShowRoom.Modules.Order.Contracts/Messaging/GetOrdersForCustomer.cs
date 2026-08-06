namespace ShowRoom.Modules.Order.Contracts.Messaging;

/// <summary>
/// Message-bus request (AMQP request/reply): "give me the orders of this customer". Sent by other
/// modules over RabbitMQ — never resolved via an in-process HTTP/DB call. The customer is referenced
/// by its public id only.
/// </summary>
public sealed record GetOrdersForCustomer(string CustomerPublicId);
