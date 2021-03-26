using Energinet.DataHub.MarketData.Domain.SeedWork;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process.Events
{
    public class GridOperatorNotified : DomainEventBase
    {
        public GridOperatorNotified(string processId)
        {
            ProcessId = processId;
        }

        public string ProcessId { get; }
    }
}
