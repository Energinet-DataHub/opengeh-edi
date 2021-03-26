using MediatR;

namespace Energinet.DataHub.MarketData.Application.Common.Commands
{
    /// <summary>
    /// CQRS command object
    /// </summary>
    public interface ICommand : IRequest
    {
    }

    /// <summary>
    /// CQRS command with result
    /// </summary>
    /// <typeparam name="TResult"><see cref="IRequest"/></typeparam>
    public interface ICommand<out TResult> : IRequest<TResult>
    {
    }
}
