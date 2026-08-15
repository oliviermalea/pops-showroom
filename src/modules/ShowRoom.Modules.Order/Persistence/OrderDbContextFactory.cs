using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ShowRoom.Modules.Order.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling (<c>dotnet ef migrations</c>). Not used at runtime,
/// where the context is configured through the module's <see cref="DatabaseModule"/>. The connection
/// string is read from configuration/environment — never hardcoded and never carrying a secret in
/// source. <c>migrations add</c> does not connect, so the password-less local fallback is sufficient;
/// provide real credentials via <c>ConnectionStrings__showroom</c> (env) for design-time
/// <c>database update</c>.
/// </summary>
public sealed class OrderDbContextFactory : IDesignTimeDbContextFactory<OrdersContext>
{
    public OrdersContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("showroom")
            ?? "Host=localhost;Port=5432;Database=showroom;Username=postgres";

        var options = new DbContextOptionsBuilder<OrdersContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new OrdersContext(options);
    }
}
