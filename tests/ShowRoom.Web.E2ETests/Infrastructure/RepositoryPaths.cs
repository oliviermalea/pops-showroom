namespace ShowRoom.Web.E2ETests.Infrastructure;

/// <summary>
/// Localise un projet du dépôt depuis le répertoire de sortie des tests.
/// </summary>
/// <remarks>
/// Nécessaire pour le front : hébergé depuis le projet de test, son <c>ContentRoot</c> pointerait sur
/// le dossier de sortie des tests, où il n'y a pas de <c>wwwroot</c>. Les assets statiques
/// répondraient alors 404 — donc ni <c>app.css</c> ni <c>blazor.web.js</c>, et une page qui reste
/// figée sur son squelette faute du script qui applique les mises à jour diffusées.
/// </remarks>
public static class RepositoryPaths
{
    private const string SolutionFile = "Pops-ShowRoom.slnx";

    public static string ProjectDirectory(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFile)))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException($"Racine du dépôt introuvable ({SolutionFile}) depuis {AppContext.BaseDirectory}.");
        }

        var projectDirectory = Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));

        return Directory.Exists(projectDirectory)
            ? projectDirectory
            : throw new DirectoryNotFoundException($"Projet introuvable : {projectDirectory}");
    }
}
