using Xunit;

// Integration tests here spin up containers (PostgreSQL, RabbitMQ) and boot the full modulith host,
// including a real-broker collection whose Wolverine cold-start warm-up must not be starved by other
// collections running in parallel. Serialising the assembly keeps these heavy, resource-bound tests
// reliable (reliability over raw speed for integration suites).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
