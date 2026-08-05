using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShowRoom.Modules.Order.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling (<c>dotnet ef migrations</c>). Not used at runtime,
/// where the context is configured through the module's <see cref="DatabaseModule"/>.
/// </summary>
public sealed class OrderDbContextFactory : IDesignTimeDbContextFactory<OrdersContext>
{
    public OrdersContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrdersContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=orderdb;Username=postgres;Password=postgres")
            .Options;

        return new OrdersContext(options);
    }
}
