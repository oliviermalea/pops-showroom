namespace ShowRoom.Modules.Customer.IntegrationTests;

using ShowRoom.Testing.Database;

/// <summary>
/// Externalises the Customer module connection string for integration tests. The key matches the
/// module fallback name read by <c>DatabaseModule.AddDatabase</c> ("Customers"); the actual test
/// DbContext is repointed to the factory container by <see cref="CustomerBusinessWebFactory"/>.
/// </summary>
internal sealed class CustomerDatabaseConfiguration : IDatabaseConfiguration
{
    private readonly string _connectionString;

    internal CustomerDatabaseConfiguration(string connectionString) => _connectionString = connectionString;

    public Dictionary<string, string?> Get() => new()
    {
        { "ConnectionStrings:Customers", _connectionString },
    };
}
