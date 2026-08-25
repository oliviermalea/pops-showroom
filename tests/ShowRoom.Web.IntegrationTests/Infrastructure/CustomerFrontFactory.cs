using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using ShowRoom.Web.Infrastructure.Api;

namespace ShowRoom.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Héberge le front Blazor et <b>rebranche son client Refit sur le serveur de test de l'API</b> :
/// chaque requête sortante du front est servie en mémoire par le host de <c>ShowRoom.Customer.Api</c>,
/// qui lit son propre conteneur PostgreSQL jetable. Aucun port n'est ouvert, aucun réseau n'est
/// traversé, et la chaîne réellement exercée est complète : SSR → façade → Refit → HTTP → endpoint →
/// EF Core → PostgreSQL.
/// </summary>
/// <remarks>
/// Le paramètre de type est <see cref="BackendApiOptions"/> et non <c>Program</c> : une
/// <see cref="WebApplicationFactory{TEntryPoint}"/> n'a besoin que d'un type <i>de l'assembly</i> de
/// l'application pour en trouver le point d'entrée. Ce projet référence deux hôtes ASP.NET Core, dont
/// les classes <c>Program</c> vivent toutes deux dans l'espace de noms global : les nommer ici serait
/// ambigu.
/// </remarks>
public sealed class CustomerFrontFactory(HttpMessageHandler backendHandler)
    : WebApplicationFactory<BackendApiOptions>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                // Adresse volontairement non routable : si un jour un appel échappait au handler de
                // test, il échouerait bruyamment au lieu de partir vers une vraie API.
                ["BackendApi:CustomerApiBaseUrl"] = "http://customer-api.test",
                ["BackendApi:ApiVersion"] = "1",
            }));

        // PostConfigureAll, et non ConfigureHttpClientDefaults : les valeurs par defaut s'appliquent
        // AVANT la configuration propre a chaque client, si bien qu'un client qui pose son propre
        // gestionnaire primaire — ce que fait le client Refit genere — ecrase la substitution et part
        // vers le reseau. Une post-configuration passe en dernier, donc elle gagne toujours.
        builder.ConfigureServices(services =>
            services.PostConfigureAll<HttpClientFactoryOptions>(options =>
                options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                    handlerBuilder.PrimaryHandler = backendHandler)));
    }
}
