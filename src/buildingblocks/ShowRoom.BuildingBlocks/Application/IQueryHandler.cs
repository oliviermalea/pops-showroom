namespace ShowRoom.BuildingBlocks.Application;

/// <summary>
/// Handles a read-only <typeparamref name="TQuery"/> and returns <typeparamref name="TResponse"/>
/// (typically a <c>Result&lt;T&gt;</c>). Resolved from DI by the feature endpoint via this interface.
/// </summary>
public interface IQueryHandler<TQuery, TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
