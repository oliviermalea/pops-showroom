namespace ShowRoom.Modules.Customer.IntegrationTests;

using ShowRoom.Testing.Database;

/// <summary>
/// Externalises the connection string for integration tests. It uses the shared Aspire key
/// (<c>showroom-business</c>) that every module's <c>DatabaseModule.AddDatabase</c> reads first, so
/// building the full modulith host (all modules' DbContexts) does not fail the connection-string
/// guard for modules that are not under test. The actual DbContext under test is repointed to the
/// factory container by <see cref="CustomerBusinessWebFactory"/>.
/// </summary>
internal sealed class CustomerDatabaseConfiguration : IDatabaseConfiguration
{
    private readonly string _connectionString;

    internal CustomerDatabaseConfiguration(string connectionString) => _connectionString = connectionString;

    public Dictionary<string, string?> Get() => new()
    {
        { "ConnectionStrings:showroom-customers", _connectionString },
    };
}
