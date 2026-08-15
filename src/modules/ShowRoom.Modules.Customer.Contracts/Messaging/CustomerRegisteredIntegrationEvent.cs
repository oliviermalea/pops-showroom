namespace ShowRoom.Modules.Customer.Contracts.Messaging;

/// <summary>
/// Integration event (AMQP): a new customer has been registered. Published by the Customer service through
/// Wolverine's transactional outbox — persisted in the same database transaction as the customer insert and
/// then delivered to RabbitMQ with retries (guaranteed at-least-once cross-service delivery). Consumers
/// react by listening to <see cref="CustomerMessagingContract.CustomerRegisteredQueue"/>.
/// </summary>
/// <remarks>
/// The customer is referenced by its <c>PublicId</c> only (never the technical id). <c>RegisteredOn</c> is
/// the business moment the customer was registered — an explicit business concept, not a raw audit column.
/// </remarks>
public sealed record CustomerRegisteredIntegrationEvent(
    string PublicId,
    string FirstName,
    string LastName,
    string Email,
    DateTimeOffset RegisteredOn);
