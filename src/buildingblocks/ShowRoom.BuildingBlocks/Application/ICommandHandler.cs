namespace ShowRoom.BuildingBlocks.Application;

/// <summary>
/// Handles a state-changing <typeparamref name="TCommand"/> and returns <typeparamref name="TResponse"/>
/// (typically a <c>Result&lt;T&gt;</c>).
/// </summary>
public interface ICommandHandler<TCommand, TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}
