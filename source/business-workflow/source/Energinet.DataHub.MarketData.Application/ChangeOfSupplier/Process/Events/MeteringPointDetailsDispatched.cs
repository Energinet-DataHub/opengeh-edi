using System;
using Energinet.DataHub.MarketData.Domain.SeedWork;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process.Events
{
    public class MeteringPointDetailsDispatched : DomainEventBase
    {
        public MeteringPointDetailsDispatched(string processId)
        {
            ProcessId = processId ?? throw new ArgumentNullException(nameof(processId));
        }

        public string ProcessId { get; }
    }
}
