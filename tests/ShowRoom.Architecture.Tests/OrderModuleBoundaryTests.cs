using ArchUnitNET.Loader;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using ShowRoom.Modules.Order;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ShowRoom.Architecture.Tests;

/// <summary>
/// Enforces the modulith layer boundaries for the Order module: the Domain stays pure (no dependency
/// on Persistence or Features), and Persistence never reaches into Features. Features may depend on
/// both Domain and Persistence (handlers use the module DbContext directly — no repository).
/// </summary>
public sealed class OrderModuleBoundaryTests
{
    private static readonly ArchUnitNET.Domain.Architecture Arch = new ArchLoader()
        .LoadAssemblies(typeof(OrderModule).Assembly)
        .Build();

    private const string DomainNamespace = "ShowRoom.Modules.Order.Domain";
    private const string FeaturesNamespace = "ShowRoom.Modules.Order.Features";
    private const string PersistenceNamespace = "ShowRoom.Modules.Order.Persistence";

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
