using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace ShowRoom.Web.Tests.Shell;

/// <summary>
/// Garde-fou de placement : la coquille technique reste sous <c>App/</c>, les écrans sous
/// <c>Features/</c> (règle §11 de <c>.claude/rules/frontend.md</c>).
/// </summary>
/// <remarks>
/// <para>Ce n'est pas une question de rangement. Les <c>_Imports.razor</c> sont <b>hiérarchiques</b> :
/// déplacer un composant change les <c>@using</c> dont il hérite, donc les composants qu'il peut
/// résoudre. Or Razor <b>ne signale pas</b> une balise de composant inconnue : il la recopie telle
/// quelle en HTML. Déplacer <c>App.razor</c> hors de <c>App/</c> transforme ainsi <c>&lt;Routes /&gt;</c>
/// en balise inerte — l'application compile, démarre, et sert une page vide sans la moindre erreur.</para>
///
/// <para>Les <c>typeof</c> ci-dessous sont le vrai filet : un déplacement casse la compilation de ce
/// test, donc bien avant l'affichage.</para>
/// </remarks>
public sealed class ShellPlacementTests
{
    private const string ShellNamespace = "ShowRoom.Web.App";
    private const string FeaturesNamespace = "ShowRoom.Web.Features";

    public static TheoryData<Type> ShellComponents() =>
    [
        typeof(global::ShowRoom.Web.App.App),
        typeof(global::ShowRoom.Web.App.Routing.Routes),
        typeof(global::ShowRoom.Web.App.Routing.NotFound),
        typeof(global::ShowRoom.Web.App.Routing.Error),
        typeof(global::ShowRoom.Web.App.Layout.MainLayout),
        typeof(global::ShowRoom.Web.App.Layout.ReconnectModal),
    ];

    [Theory]
    [MemberData(nameof(ShellComponents))]
    public void A_technical_shell_component_lives_under_the_App_namespace(Type component)
    {
        // Arrange & Act
        var sut = component.Namespace;

        // Assert
        sut.Should().StartWith(
            ShellNamespace,
            "la coquille technique doit hériter des @using de App/_Imports.razor");
    }

    [Fact]
    public void A_routable_screen_lives_under_the_Features_namespace()
    {
        // Arrange
        var assembly = typeof(global::ShowRoom.Web.App.App).Assembly;

        // Act
        var sut = RoutableComponents(assembly)
            .Where(type => !type.Namespace!.StartsWith(ShellNamespace, StringComparison.Ordinal))
            .Select(type => type.FullName!)
            .ToArray();

        // Assert — une page est un écran métier : elle n'a rien à faire ailleurs que dans Features/.
        sut.Should().NotBeEmpty("le front expose au moins une page routable");
        sut.Should().AllSatisfy(name => name.Should().StartWith(FeaturesNamespace));
    }

    [Fact]
    public void The_shell_only_makes_its_error_surfaces_routable()
    {
        // Arrange
        var assembly = typeof(global::ShowRoom.Web.App.App).Assembly;

        // Act
        var sut = RoutableComponents(assembly)
            .Where(type => type.Namespace!.StartsWith(ShellNamespace, StringComparison.Ordinal))
            .Select(type => type.FullName!)
            .ToArray();

        // Assert — Error et NotFound sont routables par nature ; toute autre page sous App/ est un
        // écran métier qui a glissé hors de Features/.
        sut.Should().BeEquivalentTo(
            [
                typeof(global::ShowRoom.Web.App.Routing.Error).FullName,
                typeof(global::ShowRoom.Web.App.Routing.NotFound).FullName,
            ]);
    }

    private static IEnumerable<Type> RoutableComponents(Assembly assembly) =>
        assembly.GetTypes().Where(type => type.GetCustomAttribute<RouteAttribute>() is not null);
}
