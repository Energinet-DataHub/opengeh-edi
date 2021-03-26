using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Application.Common.Commands;
using MediatR;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process.Commands
{
    public class NotifyCurrentSupplierHandler : ICommandHandler<NotifyCurrentSupplier>
    {
        public Task<Unit> Handle(NotifyCurrentSupplier request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
