using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShowRoom.Modules.Product.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling (<c>dotnet ef migrations</c>). Not used at runtime,
/// where the context is configured through the module's <see cref="DatabaseModule"/>.
/// </summary>
public sealed class ProductDbContextFactory : IDesignTimeDbContextFactory<ProductsContext>
{
    public ProductsContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ProductsContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=productdb;Username=postgres;Password=postgres")
            .Options;

        return new ProductsContext(options);
    }
}
