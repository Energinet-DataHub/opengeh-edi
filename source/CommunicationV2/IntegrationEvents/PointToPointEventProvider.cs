using CommunicationV2.IntegrationEvents.Publisher;
using Infrastructure.Transactions.AggregatedMeasureData;
using Microsoft.EntityFrameworkCore;

namespace CommunicationV2.IntegrationEvents;

public class PointToPointEventProvider : IPointToPointEventProvider
{
    private readonly DbContext _dbContext;
    private readonly AggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;

    public PointToPointEventProvider(DbContext dbContext, AggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository)
    {
        _dbContext = dbContext;
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
    }
    public IAsyncEnumerable<PointToPointEvent> GetAsync()
    {
        // await _aggregatedMeasureDataProcessRepository
        //     .GetByIdAsync(ProcessId.Create(request.ProcessId), cancellationToken).ConfigureAwait(false);
        //
             await _aggregatedMeasureDataProcessRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

    }
}
