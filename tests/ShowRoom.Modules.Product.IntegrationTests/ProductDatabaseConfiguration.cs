namespace ShowRoom.Modules.Product.IntegrationTests;

using ShowRoom.Testing.Database;

/// <summary>
/// Externalises the connection string for integration tests using the shared Aspire key
/// (<c>showroom</c>) that every module's <c>DatabaseModule.AddDatabase</c> reads, so
/// building the full modulith host does not fail the connection-string guard for modules not under
/// test. The Product DbContext is repointed to the factory container by
/// <see cref="ProductBusinessWebFactory"/>.
/// </summary>
internal sealed class ProductDatabaseConfiguration : IDatabaseConfiguration
{
    private readonly string _connectionString;

    internal ProductDatabaseConfiguration(string connectionString) => _connectionString = connectionString;

    public Dictionary<string, string?> Get() => new()
    {
        { "ConnectionStrings:showroom", _connectionString },
    };
}
