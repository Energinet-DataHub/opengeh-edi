using System;
using System.Threading;
using System.Threading.Tasks;
using Application.OutgoingMessages;
using Application.Transactions.AggregatedMeasureData.Notifications;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.Edi.Responses.AggregatedMeasureData;
using MediatR;

namespace Infrastructure.Transactions.AggregatedMeasureData;

public class AggregatedMeasureDataAcceptedInternalCommandHandler : IRequestHandler<AggregatedMeasureDataAcceptedInternalCommand, Unit>
{
    private readonly IOutgoingMessageStore _outgoingMessageStore;

    public AggregatedMeasureDataAcceptedInternalCommandHandler(IOutgoingMessageStore outgoingMessageStore)
    {
        _outgoingMessageStore = outgoingMessageStore;
    }

    public Task<Unit> Handle(AggregatedMeasureDataAcceptedInternalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.AggregatedMeasureDataAccepted);
        // Lav Entitet



        var transaction = new AggregatedMeasureDataForwarding(ProcessId.New());
        var aggregatedTimeSeries = AggregatedTimeSeriesRequestAccepted.Parser.ParseFrom(request.AggregatedMeasureDataAccepted.Data);
        //_transactions.Add(transaction);
        _outgoingMessageStore.Add(transaction.CreateMessage(aggregatedTimeSeries));
        return Unit.Task;

        throw new System.NotImplementedException();
    }
}
