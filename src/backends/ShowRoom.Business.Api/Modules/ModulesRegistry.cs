using ShowRoom.Modules.Order;
using ShowRoom.Modules.Product;

namespace ShowRoom.Business.Api.Modules;

/// <summary>
/// Host-side registry of module names used to drive feature-flag gating and registration.
/// </summary>
internal record class ModulesRegistry(string Value)
{
    internal static readonly ModulesRegistry Order = new(OrderConventions.ModuleName);
    internal static readonly ModulesRegistry Product = new(ProductConventions.ModuleName);

    public static implicit operator string(ModulesRegistry module) => module.Value;
}
