using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ShowRoom.Modules.Order;

internal static class ValidatorInstaller
{
    internal static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(
            typeof(ValidatorInstaller).Assembly,
            includeInternalTypes: true);

        return services;
    }
}
