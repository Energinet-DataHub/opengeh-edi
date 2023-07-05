using System;
using System.Threading;
using System.Threading.Tasks;
using Application.IncomingMessages.RequestAggregatedMeasureData;
using Domain.Transactions.AggregatedMeasureData;
using MediatR;

namespace Application.Transactions.AggregatedMeasureData;

public class RequestAggregatedMeasureDataAccepted: IRequestHandler<AggregatedMeasureDataAccepted, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;

    public RequestAggregatedMeasureDataAccepted(IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
    }

    public Task<Unit> Handle(AggregatedMeasureDataAccepted request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var process = _aggregatedMeasureDataProcessRepository.GetById(request.ProcessId) ??
                      throw new ArgumentNullException(nameof(request));

        //return Unit.Value;
    }
}
