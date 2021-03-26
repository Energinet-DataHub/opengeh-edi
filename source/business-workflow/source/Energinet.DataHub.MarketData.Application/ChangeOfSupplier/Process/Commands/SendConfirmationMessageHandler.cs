using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Application.Common.Commands;
using MediatR;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process.Commands
{
    public class SendConfirmationMessageHandler : ICommandHandler<SendConfirmationMessage>
    {
        public Task<Unit> Handle(SendConfirmationMessage request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
