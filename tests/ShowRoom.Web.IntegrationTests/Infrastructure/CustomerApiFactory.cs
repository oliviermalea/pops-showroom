extern alias customerapi;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShowRoom.Modules.Customer.Persistence;
using ShowRoom.Testing;
using Wolverine.Runtime;

namespace ShowRoom.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Héberge le service Customer sur un conteneur PostgreSQL <b>jetable</b>, monté au démarrage de la
/// classe de test et détruit à la fin : aucun test ne touche une base existante, et deux classes de
/// test ne partagent jamais leurs données.
///
/// <para>Même contrat que <c>CustomerBusinessWebFactory</c> du projet d'intégration du module : le
/// module garde UN seul DbContext (schéma métier + schéma <c>wolverine</c> de l'outbox dans la même
/// base), donc la chaîne de connexion est injectée en configuration d'hôte pour que le DbContext ET le
/// message store pointent sur ce conteneur.</para>
/// </summary>
public sealed class CustomerApiFactory : BusinessWebFactory<customerapi::Program>
{
    private IHost? host;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        host = base.CreateHost(builder);
        return host;
    }

    protected override IDictionary<string, string?> HostConfigurationOverrides() => new Dictionary<string, string?>
    {
        ["ConnectionStrings:showroom"] = _postgreSqlContainer.GetConnectionString(),
    };

    protected override void InitializeModuleTestServices(IServiceProvider serviceProvider)
        => serviceProvider.GetRequiredService<CustomersContext>().Database.Migrate();

    public override async ValueTask DisposeAsync()
    {
        if (host is not null)
        {
            await host.ClearAllWolverineStorageAsync();
        }

        await base.DisposeAsync();
    }
}
