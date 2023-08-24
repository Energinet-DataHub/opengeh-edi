using CommunicationV2.IntegrationEvents.Internal.Publisher;
using CommunicationV2.IntegrationEvents.Publisher;
using Infrastructure.Transactions.AggregatedMeasureData;
using Microsoft.EntityFrameworkCore;

namespace CommunicationV2.IntegrationEvents;

public class PointToPointEventProvider : IPointToPointEventProvider
{
    private readonly AggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;

    public PointToPointEventProvider(AggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
    }

    public async IAsyncEnumerable<PointToPointEvent> GetAsync()
    {
        var aggregatedMeasureDataProcesses = await _aggregatedMeasureDataProcessRepository
        .GetAllAsync().ConfigureAwait(false);


        foreach (var process in aggregatedMeasureDataProcesses)
        {
           yield return new PointToPointEvent()
           {
               EventIdentification = process.ProcessId,
           }
        }
    }
}
