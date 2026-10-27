namespace UnifiedServiceScheduler.Application.Common;

/// <summary>
/// Lightweight mediator abstractions to avoid third-party coupling while maintaining
/// standard MediatR signature parity.
/// </summary>
public interface ICommand<out TResponse> { }

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken ct = default);
}
