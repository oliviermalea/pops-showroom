using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace ShowRoom.Web.Tests.Shell;

/// <summary>
/// Garde-fou de placement pour le rendu WebAssembly : un composant en <c>InteractiveWebAssembly</c> doit
/// vivre dans l'assembly téléchargée par le navigateur, pas dans l'hôte.
/// </summary>
/// <remarks>
/// <para>Ce n'est pas une convention de rangement, c'est une contrainte d'exécution : l'assembly du
/// projet hôte n'est jamais envoyée au navigateur. Une page WASM déplacée dedans compile, démarre, et
/// échoue seulement à l'exécution — exactement la classe de panne silencieuse que
/// <see cref="ShellPlacementTests"/> verrouille pour la coquille.</para>
///
/// <para>Le <c>typeof</c> est le vrai filet : déplacer ces pages casse la compilation de ce test.</para>
/// </remarks>
public sealed class WebAssemblyPlacementTests
{
    private const string ClientNamespace = "ShowRoom.Web.Client.Features";

    public static TheoryData<Type> CatalogPages() =>
    [
        typeof(global::ShowRoom.Web.Client.Features.Catalog.ProductList.Page),
        typeof(global::ShowRoom.Web.Client.Features.Catalog.ProductDetail.Page),
    ];

    [Theory]
    [MemberData(nameof(CatalogPages))]
    public void A_catalogue_page_lives_in_the_assembly_the_browser_downloads(Type page)
    {
        // Arrange & Act
        var sut = page.Assembly;

        // Assert
        sut.Should().BeSameAs(
            global::ShowRoom.Web.Client.ClientAssembly.Value,
            "l'hôte ne déclare que cette assembly au routeur et au render mode WebAssembly");
    }

    [Fact]
    public void Every_routable_component_of_the_client_is_a_catalogue_screen()
    {
        // Arrange & Act
        var sut = global::ShowRoom.Web.Client.ClientAssembly.Value
            .GetTypes()
            .Where(type => type.GetCustomAttribute<RouteAttribute>() is not null)
            .Select(type => type.FullName!)
            .ToArray();

        // Assert — rien d'autre n'a de raison d'être téléchargé dans le navigateur.
        sut.Should().NotBeEmpty();
        sut.Should().AllSatisfy(name => name.Should().StartWith(ClientNamespace));
    }

    [Fact]
    public void The_shared_library_carries_no_routable_component()
    {
        // Arrange — ShowRoom.Web.Shared est une bibliothèque de briques, pas un porteur d'écrans : une
        // page qui y atterrirait serait routable depuis les deux fronts, sans propriétaire.
        var shared = typeof(global::ShowRoom.Web.Shared.Components.ApiProblemPanel).Assembly;

        // Act
        var sut = shared.GetTypes()
            .Where(type => type.GetCustomAttribute<RouteAttribute>() is not null)
            .Select(type => type.FullName!)
            .ToArray();

        // Assert
        sut.Should().BeEmpty();
    }
}
