using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShowRoom.Modules.Customer.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling (<c>dotnet ef migrations</c>). Not used at runtime,
/// where the context is configured through Aspire's Npgsql integration.
/// </summary>
public sealed class CustomerDbContextFactory : IDesignTimeDbContextFactory<CustomersContext>
{
    public CustomersContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CustomersContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=customerdb;Username=postgres;Password=postgres")
            .Options;

        return new CustomersContext(options);
    }
}
