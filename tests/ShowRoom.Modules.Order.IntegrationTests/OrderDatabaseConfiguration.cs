namespace ShowRoom.Modules.Order.IntegrationTests;

using ShowRoom.Testing.Database;

/// <summary>
/// Externalises the connection string for integration tests using the shared Aspire key
/// (<c>showroom-business</c>) that every module's <c>DatabaseModule.AddDatabase</c> reads first, so
/// building the full modulith host does not fail the connection-string guard for modules not under
/// test. The Order DbContext is repointed to the factory container by
/// <see cref="OrderBusinessWebFactory"/>.
/// </summary>
internal sealed class OrderDatabaseConfiguration : IDatabaseConfiguration
{
    private readonly string _connectionString;

    internal OrderDatabaseConfiguration(string connectionString) => _connectionString = connectionString;

    public Dictionary<string, string?> Get() => new()
    {
        { "ConnectionStrings:showroom-business", _connectionString },
    };
}
