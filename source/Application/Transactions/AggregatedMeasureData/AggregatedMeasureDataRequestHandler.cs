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
using Domain.Transactions.AggregatedMeasureData;
using MediatR;
using Serie = Domain.Transactions.AggregatedMeasureData.Serie;

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
                requestMarketActivityRecord.Id,
                requestMarketActivityRecord.SettlementSeriesVersion,
                requestMarketActivityRecord.MarketEvaluationPointType,
                requestMarketActivityRecord.MarketEvaluationSettlementMethod,
                requestMarketActivityRecord.StartDateAndOrTimeDateTime,
                requestMarketActivityRecord.EndDateAndOrTimeDateTime,
                requestMarketActivityRecord.MeteringGridAreaDomainId,
                requestMarketActivityRecord.BiddingZoneDomainId,
                requestMarketActivityRecord.EnergySupplierMarketParticipantId,
                requestMarketActivityRecord.BalanceResponsiblePartyMarketParticipantId,
                requestMessageHeader.SenderId);

        _aggregatedMeasureDataProcessRepository.Add(process);
        return Task.FromResult(Unit.Value);
    }
}
