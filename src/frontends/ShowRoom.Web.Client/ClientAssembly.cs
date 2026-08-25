using System.Reflection;

namespace ShowRoom.Web.Client;

/// <summary>
/// Marker for the WebAssembly assembly. The host needs it twice — to let the router discover the pages
/// that live here, and to declare the additional assembly to the WebAssembly render mode — and naming a
/// type is what turns a move of those pages into a compilation failure rather than a blank screen.
/// </summary>
public static class ClientAssembly
{
    public static readonly Assembly Value = typeof(ClientAssembly).Assembly;
}
