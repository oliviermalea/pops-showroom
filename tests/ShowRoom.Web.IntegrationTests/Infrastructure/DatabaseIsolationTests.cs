using AwesomeAssertions;

namespace ShowRoom.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Prouve que chaque classe de test démarre sur une base <b>neuve</b> : cette classe possède sa propre
/// fixture, donc son propre conteneur PostgreSQL, et ne voit rien de ce que les autres ont semé.
/// </summary>
/// <remarks>
/// La classe ne contient qu'UN test, volontairement : xUnit ne garantit pas l'ordre d'exécution au sein
/// d'une classe, donc une assertion « la base est vide » ne peut cohabiter avec un test qui sème.
/// </remarks>
public sealed class DatabaseIsolationTests(CustomerFrontFixture fixture) : IClassFixture<CustomerFrontFixture>
{
    [Fact]
    public async Task A_test_class_starts_on_an_empty_database()
    {
        // Act
        var html = await fixture.GetPageAsync("/customers", TestContext.Current.CancellationToken);

        // Assert — le message d'état vide, et non celui d'une recherche infructueuse.
        html.Should().Contain("Aucun client enregistré");
    }
}
