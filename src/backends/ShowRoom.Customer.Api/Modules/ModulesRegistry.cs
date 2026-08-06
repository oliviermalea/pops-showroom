using ShowRoom.Modules.Customer;

namespace ShowRoom.Customer.Api.Modules;

/// <summary>
/// Host-side registry of the module(s) this service hosts. The Customer API hosts only the Customer
/// bounded context; other contexts live in their own services and are reached over the message bus.
/// </summary>
internal record class ModulesRegistry(string Value)
{
    internal static readonly ModulesRegistry Customer = new(CustomerConventions.ModuleName);

    public static implicit operator string(ModulesRegistry module) => module.Value;
}
