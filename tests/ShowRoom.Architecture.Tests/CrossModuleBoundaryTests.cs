using ArchUnitNET.Loader;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using ShowRoom.Modules.Customer;
using ShowRoom.Modules.Order;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ShowRoom.Architecture.Tests;

/// <summary>
/// Enforces cross-module boundaries: a module may reference another module's <c>*.Contracts</c>
/// (the message-bus contract) but never its implementation. Machine-to-machine data exchange goes
/// over AMQP messaging, not by depending on another module's Domain/Features/Persistence.
/// </summary>
public sealed class CrossModuleBoundaryTests
{
    private static readonly ArchUnitNET.Domain.Architecture Arch = new ArchLoader()
        .LoadAssemblies(typeof(CustomerModule).Assembly, typeof(OrderModule).Assembly)
        .Build();

    [Fact]
    public void Customer_should_not_depend_on_order_implementation()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching("ShowRoom.Modules.Customer")
            .Should().NotDependOnAny(Types().That()
                .ResideInNamespaceMatching("ShowRoom.Modules.Order.Domain")
                .Or().ResideInNamespaceMatching("ShowRoom.Modules.Order.Features")
                .Or().ResideInNamespaceMatching("ShowRoom.Modules.Order.Persistence"));

        rule.Check(Arch);
    }
}
