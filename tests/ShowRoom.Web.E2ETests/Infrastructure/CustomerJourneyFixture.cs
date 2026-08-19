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
    /// Ouvre l'écran interactif et n'en sort qu'une fois le circuit réellement réactif.
    /// </summary>
    /// <remarks>
    /// <para>Le WebSocket ouvert ne suffit pas : entre sa connexion et l'attachement des gestionnaires
    /// d'évènements, une saisie est purement <b>perdue</b> — aucune attente ultérieure ne la rattrape.
    /// Le premier circuit paie en plus le JIT du chemin interactif côté serveur.</para>
    ///
    /// <para>La sonde est donc <b>réessayée</b> : c'est le seul moyen de distinguer « pas encore prêt »
    /// de « cassé », et cela évite de saupoudrer les parcours de temporisations fixes. Une fois ce coût
    /// payé, les circuits suivants réagissent immédiatement et les tests peuvent interagir sans
    /// précaution particulière.</para>
    /// </remarks>
    private async Task WarmUpInteractivityAsync()
    {
        const int attempts = 20;

        var page = await NewPageAsync();

        try
        {
            await page.GotoInteractiveAsync("/customers/new");

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                await page.FillAsync("#email", $"pas-un-email-{attempt}");
                await page.ClickAsync("#firstName");

                try
                {
                    await page.Locator(".validation-message").First.WaitForAsync(
                        new LocatorWaitForOptions { Timeout = 2_000 });

                    return;
                }
                catch (TimeoutException)
                {
                    // Le circuit n'a pas encore attaché ses gestionnaires : on rejoue la saisie.
                }
            }

            throw new InvalidOperationException(
                $"Le circuit interactif n'a pas réagi après {attempts} tentatives : l'écran de création " +
                "n'est pas exploitable, inutile de lancer les parcours.");
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
