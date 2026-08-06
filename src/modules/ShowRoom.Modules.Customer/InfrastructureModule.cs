using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShowRoom.BuildingBlocks.Time;
using ShowRoom.Modules.Customer.Messaging;
using ShowRoom.Modules.Customer.Persistence;

namespace ShowRoom.Modules.Customer;

/// <summary>
/// Registers the Customer module's infrastructure: EF Core (via <see cref="DatabaseModule"/>),
/// time provider and FluentValidation validators. Handlers use the DbContext directly (no repository).
/// </summary>
public static class InfrastructureModule
{
    public static IHostApplicationBuilder AddInfrastructureModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabase(builder.Configuration);

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Outbound anti-corruption gateway to the Order module (AMQP request/reply, not HTTP).
        builder.Services.AddScoped<IOrderQueryGateway, WolverineOrderQueryGateway>();

        builder.Services.AddValidators();

        return builder;
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseDatabase();

        return applicationBuilder;
    }
}
