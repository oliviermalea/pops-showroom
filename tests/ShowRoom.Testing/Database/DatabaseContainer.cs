namespace ShowRoom.Testing.Database;

using Testcontainers.PostgreSql;

/// <summary>A disposable PostgreSQL container usable as an xUnit fixture.</summary>
public sealed class DatabaseContainer : IAsyncLifetime
{
    private const string Username = "postgres";
    private const string Password = "postgres";
    private const string Database = "showroom-business";
    private PostgreSqlContainer? _container;

    public string? ConnectionString { get; private set; }

    public async ValueTask InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase(Database)
            .WithUsername(Username)
            .WithPassword(Password)
            .Build();

        await _container.StartAsync();

        ConnectionString = _container.GetConnectionString();
    }

    public async ValueTask DisposeAsync() => await _container!.DisposeAsync();
}
