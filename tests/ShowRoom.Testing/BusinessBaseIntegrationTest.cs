namespace ShowRoom.Testing;

using Microsoft.Extensions.DependencyInjection;
using ShowRoom.Testing.Database;

/// <summary>
/// Base class for integration tests: exposes the factory, the shared database container and a
/// per-test DI scope.
/// </summary>
public abstract class BusinessBaseIntegrationTest<TFactory, TEntryPoint> : IDisposable
    where TFactory : BusinessWebFactory<TEntryPoint>
    where TEntryPoint : class
{
    protected BusinessBaseIntegrationTest(
        TFactory factory,
        DatabaseContainer databaseContainer)
    {
        Factory = factory;
        DatabaseContainer = databaseContainer;
        Scope = factory.Services.CreateScope();
    }

    protected TFactory Factory { get; }

    protected DatabaseContainer DatabaseContainer { get; }

    protected IServiceScope Scope { get; }

    protected IServiceProvider Services => Scope.ServiceProvider;

    public void Dispose()
    {
        Scope.Dispose();
        GC.SuppressFinalize(this);
    }
}
