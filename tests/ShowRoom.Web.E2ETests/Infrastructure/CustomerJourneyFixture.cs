using System.Net.Http.Json;
using Microsoft.Playwright;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace ShowRoom.Web.E2ETests.Infrastructure;

/// <summary>
/// Monte la pile complète pour les parcours navigateur : conteneurs PostgreSQL et RabbitMQ jetables →
/// service Customer → front Blazor → Chromium. Les deux applications tournent comme de vrais processus,
/// exactement comme en déploiement.
///
/// <para>C'est le seul niveau qui exerce ce que ni bUnit ni les tests d'intégration HTTP n'atteignent :
/// le <b>circuit interactif</b> de Blazor et le <b>JavaScript de la coquille</b>. Il est volontairement
/// réservé à quelques parcours critiques — il est lent et dépend de Docker.</para>
/// </summary>
public sealed class CustomerJourneyFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("showroom")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer broker = new RabbitMqBuilder()
        .WithImage("rabbitmq:4.1-management")
        .Build();

    private ApplicationProcess api = null!;
    private ApplicationProcess front = null!;
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private HttpClient apiClient = null!;

    /// <summary>Adresse du front, telle qu'un navigateur la voit.</summary>
    public string FrontAddress => front.BaseAddress;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(database.StartAsync(), broker.StartAsync());

        // Le module Customer publie son IntegrationEvent par l'outbox transactionnel : Wolverine doit
        // donc être réellement configuré. En processus, on ne peut pas neutraliser les transports par
        // code comme le font les tests d'intégration — d'où un vrai broker jetable.
        api = await ApplicationProcess.StartAsync(
            "src/backends/ShowRoom.Customer.Api",
            "ShowRoom.Customer.Api",
            new Dictionary<string, string>
            {
                ["ConnectionStrings__showroom"] = database.GetConnectionString(),
                ["ConnectionStrings__messaging"] = broker.GetConnectionString(),
                ["FeatureManagement__Customer"] = "true",
                ["Messaging__Enabled"] = "true",
                ["Messaging__Transport"] = "RabbitMq",
                ["Messaging__RabbitMqConnectionName"] = "messaging",
                ["Messaging__UseTransactionalOutbox"] = "true",
                ["Messaging__MessageStoreConnectionName"] = "showroom",
                ["Messaging__MessageStoreSchema"] = "wolverine",
            },
            readinessPath: "/api/status");

        front = await ApplicationProcess.StartAsync(
            "src/frontends/ShowRoom.Web",
            "ShowRoom.Web",
            new Dictionary<string, string>
            {
                ["BackendApi__CustomerApiBaseUrl"] = api.BaseAddress,
                ["BackendApi__ApiVersion"] = "1",
            },
            readinessPath: "/");

        apiClient = new HttpClient { BaseAddress = new Uri(api.BaseAddress) };

        // Idempotent : ne télécharge que si le navigateur n'est pas déjà en cache.
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Installation de Chromium échouée (code {exitCode}).");
        }

        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

        // Les assertions Playwright expirent à 5 s par défaut : trop court pour un premier rendu qui
        // paie le JIT et la construction du modèle EF. Un délai trop serré produirait des échecs
        // intermittents plutôt que des régressions réelles.
        Assertions.SetDefaultExpectTimeout(15_000);

        await WarmUpInteractivityAsync();
    }

    /// <summary>
    /// Paie une fois le coût du tout premier circuit (JIT du chemin interactif côté serveur), pour que
    /// les parcours ne le paient pas dans leurs assertions.
    /// </summary>
    /// <remarks>
    /// La course « WebSocket ouvert mais gestionnaires pas encore attachés » n'est PAS traitée ici :
    /// elle se rejoue à chaque nouveau circuit, donc à chaque navigation. C'est
    /// <see cref="InteractivePageExtensions.GotoCreateCustomerAsync"/> qui la neutralise, au bon endroit.
    /// </remarks>
    private async Task WarmUpInteractivityAsync()
    {
        var page = await NewPageAsync();

        try
        {
            await page.GotoCreateCustomerAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>Ouvre une page neuve (contexte isolé : ni cookie ni état partagé entre parcours).</summary>
    public async Task<IPage> NewPageAsync(int width = 1280, int height = 800)
    {
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            BaseURL = FrontAddress,
        });

        return await context.NewPageAsync();
    }

    /// <summary>Sème un client par l'API réelle, quand le parcours a besoin de données préexistantes.</summary>
    public async Task<string> SeedCustomerAsync(
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        var response = await apiClient.PostAsJsonAsync("/api/v1/customers", new
        {
            firstName,
            lastName,
            email = $"{Guid.NewGuid():N}@showroom.test",
            phone = (string?)null,
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<string>(cancellationToken))!;
    }

    public async ValueTask DisposeAsync()
    {
        if (browser is not null)
        {
            await browser.DisposeAsync();
        }

        playwright?.Dispose();
        apiClient?.Dispose();

        if (front is not null)
        {
            await front.DisposeAsync();
        }

        if (api is not null)
        {
            await api.DisposeAsync();
        }

        await Task.WhenAll(database.DisposeAsync().AsTask(), broker.DisposeAsync().AsTask());
    }
}
