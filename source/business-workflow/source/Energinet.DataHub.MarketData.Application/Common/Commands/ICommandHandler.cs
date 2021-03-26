using MediatR;

namespace Energinet.DataHub.MarketData.Application.Common.Commands
{
    /// <summary>
    /// Handler for CQRS command
    /// </summary>
    /// <typeparam name="TCommand"><see cref="ICommand{TResult}"/></typeparam>
    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
        where TCommand : ICommand
    {
    }

    /// <summary>
    /// Handler for CQRS command with result
    /// </summary>
    /// <typeparam name="TCommand"><see cref="ICommand{TResult}"/></typeparam>
    /// <typeparam name="TResult">Type of result returned by handler</typeparam>
    public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
    }
}
