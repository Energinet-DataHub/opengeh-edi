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
using Application.IncomingMessages.RequestAggregatedMeasureData;
using Domain.Actors;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using MediatR;
using NodaTime.Text;

namespace Application.Transactions.AggregatedMeasureData;

public class AggregatedMeasureDataRequestHandler : IRequestHandler<RequestAggregatedMeasureDataTransaction, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;

    public AggregatedMeasureDataRequestHandler(IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
    }

    public Task<Unit> Handle(RequestAggregatedMeasureDataTransaction request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var requestMessageHeader = request.MessageHeader;
        var requestMarketActivityRecord = request.MarketActivityRecord;

        var process = new AggregatedMeasureDataProcess(
            ProcessId.New(),
            BusinessTransactionId.Create(requestMarketActivityRecord.Id),
            ActorNumber.Create(requestMessageHeader.SenderId),
            requestMarketActivityRecord.SettlementSeriesVersion,
            requestMarketActivityRecord.MarketEvaluationPointType,
            requestMarketActivityRecord.MarketEvaluationSettlementMethod,
            InstantPattern.General.Parse(requestMarketActivityRecord.StartDateAndOrTimeDateTime).GetValueOrThrow(),
            InstantPattern.General.Parse(requestMarketActivityRecord.EndDateAndOrTimeDateTime).GetValueOrThrow(),
            requestMarketActivityRecord.MeteringGridAreaDomainId,
            requestMarketActivityRecord.EnergySupplierMarketParticipantId,
            requestMarketActivityRecord.BalanceResponsiblePartyMarketParticipantId);

        _aggregatedMeasureDataProcessRepository.Add(process);
        return Task.FromResult(Unit.Value);
    }
}
