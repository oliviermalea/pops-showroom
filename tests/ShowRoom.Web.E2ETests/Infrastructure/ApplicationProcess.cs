using System.Diagnostics;
using System.Net.Sockets;

namespace ShowRoom.Web.E2ETests.Infrastructure;

/// <summary>
/// Lance une application du dépôt comme un <b>vrai processus</b>, depuis son propre dossier de sortie.
/// </summary>
/// <remarks>
/// <para>Choix assumé après avoir essayé l'hébergement in-process (<c>WebApplicationFactory</c> +
/// Kestrel) : cette voie ne sert pas les assets statiques du front, parce que le manifeste
/// <c>ShowRoom.Web.staticwebassets.endpoints.json</c> n'est pas copié dans la sortie du projet de test
/// — <c>blazor.web.js</c> et <c>app.css</c> répondaient 500, et la page restait figée sur son squelette
/// faute du script qui applique les mises à jour diffusées. Or c'est précisément ce que Playwright doit
/// exercer.</para>
///
/// <para>Lancer l'application depuis SON dossier de sortie règle le problème à la racine : content
/// root, wwwroot, manifestes et configuration sont exactement ceux d'un déploiement. On teste donc
/// l'application telle qu'elle tourne, pas une variante reconstituée.</para>
/// </remarks>
public sealed class ApplicationProcess : IAsyncDisposable
{
    private readonly Process process;

    private ApplicationProcess(Process process, string baseAddress)
    {
        this.process = process;
        BaseAddress = baseAddress;
    }

    /// <summary>Adresse écoutée par l'application, du type <c>http://127.0.0.1:49812</c>.</summary>
    public string BaseAddress { get; }

    /// <summary>
    /// Démarre l'application et attend qu'elle réponde sur <paramref name="readinessPath"/>.
    /// </summary>
    public static async Task<ApplicationProcess> StartAsync(
        string projectRelativePath,
        string assemblyName,
        IDictionary<string, string> environment,
        string readinessPath,
        CancellationToken cancellationToken = default)
    {
        var outputDirectory = Path.Combine(
            RepositoryPaths.ProjectDirectory(projectRelativePath),
            "bin",
            BuildConfiguration,
            "net10.0");

        var assemblyPath = Path.Combine(outputDirectory, $"{assemblyName}.dll");

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException(
                $"{assemblyName} n'est pas compilé ({assemblyPath}). Les tests E2E lancent les applications " +
                "depuis leur propre sortie : construire la solution avant de les exécuter.",
                assemblyPath);
        }

        var port = FreeTcpPort();
        var baseAddress = $"http://127.0.0.1:{port}";

        var startInfo = new ProcessStartInfo("dotnet", $"\"{assemblyPath}\"")
        {
            WorkingDirectory = outputDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.Environment["ASPNETCORE_URLS"] = baseAddress;
        // Environnement Development, comme un `dotnet run` local : c'est ce mode qui cable les assets
        // statiques (wwwroot, modules JS) tels que le navigateur les attend. Les surcharges passent par
        // des variables d'environnement, qui l'emportent sur appsettings.Development.json — contrairement
        // a la configuration d'hote, ce qui evite d'avoir a inventer un environnement dedie.
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

        foreach (var (key, value) in environment)
        {
            startInfo.Environment[key] = value;
        }

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Impossible de démarrer {assemblyName}.");

        // Les sorties sont drainées : un processus dont les tampons saturent se bloque.
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var application = new ApplicationProcess(process, baseAddress);

        try
        {
            await application.WaitUntilReadyAsync(readinessPath, cancellationToken);
        }
        catch
        {
            await application.DisposeAsync();
            throw;
        }

        return application;
    }

    private static string BuildConfiguration =>
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    private static int FreeTcpPort()
    {
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private async Task WaitUntilReadyAsync(string readinessPath, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"L'application s'est arrêtée au démarrage (code {process.ExitCode}).");
            }

            try
            {
                var response = await client.GetAsync($"{BaseAddress}{readinessPath}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Pas encore à l'écoute.
            }
            catch (TaskCanceledException)
            {
                // Démarrage encore en cours.
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException($"L'application n'a pas répondu sur {BaseAddress}{readinessPath} en 90 s.");
    }

    public async ValueTask DisposeAsync()
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }

        process.Dispose();
    }
}
