using AwesomeAssertions;

namespace ShowRoom.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Contrepartie de <see cref="DatabaseIsolationTests"/> : ce que cette classe sème lui est propre et
/// n'atteint aucune autre classe — la fixture, donc le conteneur, lui est dédiée.
/// </summary>
public sealed class SeededDataIsolationTests(CustomerFrontFixture fixture) : IClassFixture<CustomerFrontFixture>
{
    [Fact]
    public async Task What_a_class_seeds_stays_within_that_class()
    {
        // Arrange
        await fixture.SeedCustomerAsync("Sophie", "Wilson", cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var html = await fixture.GetPageAsync("/customers", TestContext.Current.CancellationToken);

        // Assert — aucun client semé par les autres classes n'apparaît.
        html.Should().Contain("Sophie Wilson")
            .And.NotContain("Ada Lovelace")
            .And.NotContain("Grace Hopper");
    }
}
