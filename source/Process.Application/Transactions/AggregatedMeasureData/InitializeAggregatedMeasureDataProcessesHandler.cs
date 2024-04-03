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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Interfaces;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;

public class InitializeAggregatedMeasureDataProcessesHandler : IRequestHandler<InitializeAggregatedMeasureDataProcessesCommand, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;

    public InitializeAggregatedMeasureDataProcessesHandler(
        IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
    }

    public Task<Unit> Handle(
        InitializeAggregatedMeasureDataProcessesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Dto);

        CreateAggregatedMeasureDataProcess(request.Dto);

        return Task.FromResult(Unit.Value);
    }

    private void CreateAggregatedMeasureDataProcess(
        RequestAggregatedMeasureDataDto dto)
    {
        var actorSenderNumber = ActorNumber.Create(dto.SenderNumber);
        var businessReason = BusinessReason.FromCode(dto.BusinessReason);
        var messageId = MessageId.Create(dto.MessageId);

        foreach (var serie in dto.Series)
        {
            var settlementVersion = !string.IsNullOrWhiteSpace(serie.SettlementVersion)
                ? SettlementVersion.FromCode(serie.SettlementVersion)
                : null;

            _aggregatedMeasureDataProcessRepository.Add(
                new AggregatedMeasureDataProcess(
                    ProcessId.New(),
                    BusinessTransactionId.Create(serie.Id),
                    actorSenderNumber,
                    dto.SenderRoleCode,
                    businessReason,
                    messageId,
                    serie.MarketEvaluationPointType,
                    serie.MarketEvaluationSettlementMethod,
                    serie.StartDateAndOrTimeDateTime,
                    serie.EndDateAndOrTimeDateTime,
                    serie.MeteringGridAreaDomainId,
                    serie.EnergySupplierMarketParticipantId,
                    serie.BalanceResponsiblePartyMarketParticipantId,
                    settlementVersion));
        }
    }
}
