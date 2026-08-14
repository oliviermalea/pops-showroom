using ArchUnitNET.Loader;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using ShowRoom.Modules.Product;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ShowRoom.Architecture.Tests;

/// <summary>
/// Enforces the modulith layer boundaries for the Product module: the Domain stays pure (no dependency
/// on Persistence or Features), and Persistence never reaches into Features. Features may depend on
/// both Domain and Persistence (handlers use the module DbContext directly — no repository).
/// </summary>
public sealed class ProductModuleBoundaryTests
{
    private static readonly ArchUnitNET.Domain.Architecture Arch = new ArchLoader()
        .LoadAssemblies(typeof(ProductModule).Assembly)
        .Build();

    private const string DomainNamespace = "ShowRoom.Modules.Product.Domain";
    private const string FeaturesNamespace = "ShowRoom.Modules.Product.Features";
    private const string PersistenceNamespace = "ShowRoom.Modules.Product.Persistence";

    [Fact]
    public void Domain_should_not_depend_on_persistence()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(DomainNamespace)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(PersistenceNamespace));

        rule.Check(Arch);
    }

    [Fact]
    public void Domain_should_not_depend_on_features()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(DomainNamespace)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(FeaturesNamespace));

        rule.Check(Arch);
    }

    [Fact]
    public void Persistence_should_not_depend_on_features()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(PersistenceNamespace)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(FeaturesNamespace));

        rule.Check(Arch);
    }
}
