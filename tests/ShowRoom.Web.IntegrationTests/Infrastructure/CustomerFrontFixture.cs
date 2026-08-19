using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ShowRoom.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Assemble la chaîne complète pour une classe de test : conteneur PostgreSQL neuf → API Customer →
/// front Blazor. Le jeu de données est <b>semé par les tests eux-mêmes</b>, à travers l'API publique,
/// et disparaît avec le conteneur.
/// </summary>
public sealed class CustomerFrontFixture : IAsyncLifetime
{
    private CustomerApiFactory apiFactory = null!;
    private CustomerFrontFactory frontFactory = null!;

    /// <summary>Client HTTP du front : c'est lui qui rend le HTML SSR que les tests assertent.</summary>
    public HttpClient Front { get; private set; } = null!;

    /// <summary>Client HTTP de l'API, pour semer le jeu de données du scénario.</summary>
    public HttpClient Api { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        apiFactory = new CustomerApiFactory();
        await apiFactory.InitializeAsync();

        Api = apiFactory.CreateClient();

        // Le handler du TestServer de l'API devient le transport du client Refit du front.
        frontFactory = new CustomerFrontFactory(apiFactory.Server.CreateHandler());
        Front = frontFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    /// <summary>
    /// Récupère une page du front, <b>décodée</b>. Razor encode les entités HTML (« é » devient
    /// « &amp;#xE9; ») : sans décodage, une assertion sur un texte accentué échouerait alors que la page
    /// est correcte.
    /// </summary>
    public async Task<string> GetPageAsync(string url, CancellationToken cancellationToken = default)
        => WebUtility.HtmlDecode(await Front.GetStringAsync(url, cancellationToken));

    /// <summary>Crée un client via l'API réelle et renvoie son PublicId.</summary>
    public async Task<string> SeedCustomerAsync(
        string firstName,
        string lastName,
        string? email = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Api.PostAsJsonAsync("/api/v1/customers", new
        {
            firstName,
            lastName,
            email = email ?? $"{Guid.NewGuid():N}@showroom.test",
            phone = (string?)null,
        }, cancellationToken);

        response.EnsureSuccessStatusCode();

        // Le corps du 201 est l'identifiant public, sérialisé en chaîne JSON.
        return (await response.Content.ReadFromJsonAsync<string>(cancellationToken))!;
    }

    public async ValueTask DisposeAsync()
    {
        Front?.Dispose();
        Api?.Dispose();

        if (frontFactory is not null)
        {
            await frontFactory.DisposeAsync();
        }

        if (apiFactory is not null)
        {
            await apiFactory.DisposeAsync();
        }
    }
}
