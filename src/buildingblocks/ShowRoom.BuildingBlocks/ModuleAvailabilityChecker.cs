using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;

namespace ShowRoom.BuildingBlocks;

public static class ModuleAvailabilityChecker
{
    public static bool IsModuleEnabled(this IConfiguration configuration, string module)
        => configuration.GetValue<bool>($"FeatureManagement:{module}");

    public static bool IsModuleEnabled(this IServiceCollection services, string module)
    {
        var buildServiceProvider = services.BuildServiceProvider();
        var featureManager = buildServiceProvider.GetRequiredService<IFeatureManager>();

        return featureManager.IsEnabledAsync(module).GetAwaiter().GetResult();
    }

    public static bool IsModuleEnabled(this IApplicationBuilder applicationBuilder, string module)
        => applicationBuilder.ApplicationServices
            .GetRequiredService<IConfiguration>()
            .IsModuleEnabled(module);

    public static bool IsModuleEnabled(this IHostApplicationBuilder applicationBuilder, string module)
        => applicationBuilder.Configuration.IsModuleEnabled(module);
}
