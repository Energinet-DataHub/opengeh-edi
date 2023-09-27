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
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.Transactions;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using MediatR;
using Receiver =
    Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestAggregatedMeasureData.
    RequestAggregatedMeasureDataReceiver;

namespace Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;

public class InitializeAggregatedMeasureDataProcessesHandler
    : IRequestHandler<InitializeAggregatedMeasureDataProcessesCommand, Result>
{
    private readonly Receiver _messageReceiver;
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;

    public InitializeAggregatedMeasureDataProcessesHandler(
        Receiver messageReceiver,
        IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository)
    {
        _messageReceiver = messageReceiver;
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
    }

    public async Task<Result> Handle(
        InitializeAggregatedMeasureDataProcessesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.MessageResult.IncomingMarketDocument);

        var marketDocument =
            RequestAggregatedMeasureDocumentFactory.Created(request.MessageResult.IncomingMarketDocument);

        var result = await _messageReceiver.ReceiveAsync(marketDocument, cancellationToken)
            .ConfigureAwait(false);

        if (result.Errors.Count == 0)
            CreateAggregatedMeasureDataProcess(RequestAggregatedMeasureDocumentFactory.Created(request.MessageResult.IncomingMarketDocument));

        return result;
    }

    private void CreateAggregatedMeasureDataProcess(
        RequestAggregatedMeasureDataMarketMessage marketMessage)
    {
        foreach (var serie in marketMessage.MarketTransactions)
        {
            _aggregatedMeasureDataProcessRepository.Add(
                new AggregatedMeasureDataProcess(
                    ProcessId.New(),
                    BusinessTransactionId.Create(serie.Id),
                    marketMessage.SenderNumber,
                    marketMessage.SenderRole.Code,
                    marketMessage.BusinessReason.Name,
                    serie.MarketEvaluationPointType,
                    serie.MarketEvaluationSettlementMethod,
                    serie.StartDateAndOrTimeDateTime,
                    serie.EndDateAndOrTimeDateTime,
                    serie.MeteringGridAreaDomainId,
                    serie.EnergySupplierMarketParticipantId,
                    serie.BalanceResponsiblePartyMarketParticipantId));
        }
    }
}
