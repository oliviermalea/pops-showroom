namespace ShowRoom.Testing.Database;

/// <summary>Supplies the connection-string settings a module needs during integration tests.</summary>
public interface IDatabaseConfiguration
{
    Dictionary<string, string?> Get();
}
