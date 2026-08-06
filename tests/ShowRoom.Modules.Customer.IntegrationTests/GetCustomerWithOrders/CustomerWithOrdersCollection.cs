namespace ShowRoom.Modules.Customer.IntegrationTests.GetCustomerWithOrders;

/// <summary>
/// Groups the real-broker end-to-end tests into a single, non-parallel collection that shares ONE
/// <see cref="CustomerWithOrdersBusinessWebFactory"/> (hence one host + one RabbitMQ container). This
/// avoids spinning up multiple brokers in parallel and the resource contention that would push the
/// request/reply round-trip past Wolverine's 5s timeout.
/// </summary>
[CollectionDefinition("CustomerWithOrders", DisableParallelization = true)]
public sealed class CustomerWithOrdersCollection : ICollectionFixture<CustomerWithOrdersBusinessWebFactory>;
