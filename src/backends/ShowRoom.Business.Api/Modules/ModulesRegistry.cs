using ShowRoom.Modules.Customer;

namespace ShowRoom.Business.Api.Modules;

/// <summary>
/// Host-side registry of module names used to drive feature-flag gating and registration.
/// </summary>
internal record class ModulesRegistry(string Value)
{
    internal static readonly ModulesRegistry Customer = new(CustomerConventions.ModuleName);

    public static implicit operator string(ModulesRegistry module) => module.Value;
}
