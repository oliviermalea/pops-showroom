using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShowRoom.BuildingBlocks.Time;
using ShowRoom.Modules.Product.Persistence;

namespace ShowRoom.Modules.Product;

/// <summary>
/// Registers the Product module's infrastructure: EF Core (via <see cref="DatabaseModule"/>),
/// time provider and FluentValidation validators. Handlers use the DbContext directly (no repository).
/// </summary>
public static class InfrastructureModule
{
    public static IHostApplicationBuilder AddInfrastructureModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabase(builder.Configuration);

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        builder.Services.AddValidators();

        return builder;
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseDatabase();

        return applicationBuilder;
    }
}
