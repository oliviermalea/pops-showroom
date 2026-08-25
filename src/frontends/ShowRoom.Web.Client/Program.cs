using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShowRoom.Web.Client.Infrastructure.Api;

// Point d'entrée exécuté DANS LE NAVIGATEUR. Il ne câble que ce dont les écrans WebAssembly ont besoin ;
// la configuration provient de wwwroot/appsettings.json, servi comme un fichier statique.
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Même méthode d'inscription que côté hôte : le composant s'exécute des deux côtés (préretour puis
// navigateur), donc ses dépendances doivent exister dans les deux conteneurs.
builder.Services.AddCatalog(builder.Configuration);

await builder.Build().RunAsync();
