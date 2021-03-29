using Energinet.DataHub.MarketData.Application.Common.Commands;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process.Commands
{
    public class ChangeSupplier : IInternalCommand
    {
        public ChangeSupplier()
        {
        }

        public ChangeSupplier(ProcessId processId)
        {
            ProcessId = processId;
        }

        public ProcessId? ProcessId { get; set; }
    }
}
