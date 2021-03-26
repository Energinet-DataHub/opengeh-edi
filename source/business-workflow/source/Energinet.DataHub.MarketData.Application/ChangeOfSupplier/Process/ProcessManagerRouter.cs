using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process.Events;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Events;
using MediatR;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process
{
    public class ProcessManagerRouter
        : INotificationHandler<EnergySupplierChangeRegistered>,
            INotificationHandler<ConfirmationMessageDispatched>,
            INotificationHandler<MeteringPointDetailsDispatched>,
            INotificationHandler<ConsumerDetailsDispatched>,
            INotificationHandler<GridOperatorNotified>,
            INotificationHandler<CurrentSupplierNotified>,
            INotificationHandler<EnergySupplierChanged>
    {
        public Task Handle(EnergySupplierChangeRegistered notification, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task Handle(ConfirmationMessageDispatched notification, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task Handle(CurrentSupplierNotified notification, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task Handle(EnergySupplierChanged notification, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task Handle(MeteringPointDetailsDispatched notification, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task Handle(ConsumerDetailsDispatched notification, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task Handle(GridOperatorNotified notification, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
