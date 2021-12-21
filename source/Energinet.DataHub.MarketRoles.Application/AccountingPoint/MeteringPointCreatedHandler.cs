using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Energinet.DataHub.MarketRoles.Application.AccountingPoint
{
    public class MeteringPointCreatedHandler : INotificationHandler<MeteringPointCreated>
    {
        public Task Handle(MeteringPointCreated notification, CancellationToken cancellationToken)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            return Task.CompletedTask;
        }
    }
}
