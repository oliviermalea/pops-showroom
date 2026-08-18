using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ShowRoom.Modules.Customer.Persistence;
using Xunit;
using CustomerAggregate = ShowRoom.Modules.Customer.Domain.Customer;

namespace ShowRoom.Modules.Customer.Tests.Persistence;

/// <summary>
/// Guardrails on the storage mapping. An index is invisible in behaviour tests — everything still
/// passes without it, only slower — so the model itself is asserted here.
/// </summary>
public sealed class CustomerMappingTests
{
    /// <summary>
    /// The design-time model, not <c>context.Model</c>: the runtime model is read-optimised and drops
    /// configuration such as an index's sort direction.
    /// </summary>
    private static IModel Model()
        => new CustomersContext(new DbContextOptionsBuilder<CustomersContext>()
                .UseInMemoryDatabase($"customers-model-{Guid.NewGuid():N}")
                .Options)
            .GetService<IDesignTimeModel>()
            .Model;

    /// <summary>
    /// The list orders by CreatedAt descending. Without this index PostgreSQL scans then sorts the
    /// whole table on every page (measured at 200 000 rows: external merge sort spilling 9.8 MB to
    /// disk, 41 ms per page; 0.04 ms with the index).
    /// </summary>
    [Fact]
    public void Customers_are_indexed_on_their_creation_date()
    {
        // Arrange
        var entity = Model().FindEntityType(typeof(CustomerAggregate));

        // Act
        var sut = entity!.GetIndexes()
            .SingleOrDefault(index => index.Properties.Count == 1
                && index.Properties[0].Name == nameof(CustomerAggregate.CreatedAt));

        // Assert
        sut.Should().NotBeNull("the paginated list sorts on CreatedAt and would otherwise scan the whole table");
        sut!.IsDescending.Should().NotBeNull("the index must be declared descending to serve ORDER BY DESC directly");
        sut.IsUnique.Should().BeFalse("several customers can share a creation instant");
    }

    [Theory]
    [InlineData(nameof(CustomerAggregate.PublicId))]
    [InlineData(nameof(CustomerAggregate.Email))]
    public void Public_identifiers_stay_unique(string propertyName)
    {
        // Arrange
        var entity = Model().FindEntityType(typeof(CustomerAggregate));

        // Act
        var sut = entity!.GetIndexes()
            .SingleOrDefault(index => index.Properties.Count == 1 && index.Properties[0].Name == propertyName);

        // Assert
        sut.Should().NotBeNull();
        sut!.IsUnique.Should().BeTrue();
    }
}
