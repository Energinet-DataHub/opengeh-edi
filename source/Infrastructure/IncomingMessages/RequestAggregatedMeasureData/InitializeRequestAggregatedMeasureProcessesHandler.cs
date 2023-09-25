// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.Transactions;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using MediatR;
using NodaTime.Text;
using Receiver =
    Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestAggregatedMeasureData.
    RequestAggregatedMeasureDataReceiver;

namespace Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;

public class InitializeRequestAggregatedMeasureProcessesHandler
    : IRequestHandler<ReceiveAggregatedMeasureDataRequestCommand, Result>
{
    private readonly Receiver _messageReceiver;
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;

    public InitializeRequestAggregatedMeasureProcessesHandler(
        Receiver messageReceiver,
        IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository)
    {
        _messageReceiver = messageReceiver;
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
    }

    public async Task<Result> Handle(
        ReceiveAggregatedMeasureDataRequestCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _messageReceiver.ReceiveAsync(request.MessageResult, cancellationToken)
            .ConfigureAwait(false);

        if (result.Errors.Count == 0 && request.MessageResult.IncomingMarketDocument != null)
            CreateAggregatedMeasureDataProcess(RequestAggregatedMeasureDocumentFactory.Created(request.MessageResult.IncomingMarketDocument));

        return result;
    }

    private void CreateAggregatedMeasureDataProcess(
        RequestAggregatedMeasureDocument marketDocument)
    {
        foreach (var serie in marketDocument.Series)
        {
            _aggregatedMeasureDataProcessRepository.Add(
                new AggregatedMeasureDataProcess(
                    ProcessId.New(),
                    BusinessTransactionId.Create(serie.Id),
                    ActorNumber.Create(marketDocument.SenderId),
                    marketDocument.SenderRole,
                    marketDocument.BusinessReason,
                    serie.MarketEvaluationPointType,
                    serie.MarketEvaluationSettlementMethod,
                    InstantPattern.General.Parse(serie.StartDateAndOrTimeDateTime)
                        .GetValueOrThrow(),
                    serie.EndDateAndOrTimeDateTime is not null ? InstantPattern.General.Parse(serie.EndDateAndOrTimeDateTime).GetValueOrThrow() : null,
                    serie.MeteringGridAreaDomainId,
                    serie.EnergySupplierMarketParticipantId,
                    serie.BalanceResponsiblePartyMarketParticipantId));
        }
    }
}
