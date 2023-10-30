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
using Energinet.DataHub.EDI.Application.IncomingMessages;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Common;
using Energinet.DataHub.EDI.Domain.Transactions;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common;
using MediatR;

namespace Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;

public class InitializeAggregatedMeasureDataProcessesHandler
    : IRequestHandler<InitializeAggregatedMeasureDataProcessesCommand, Result>
{
    private readonly RequestAggregatedMeasureDataValidator _messageValidator;
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;

    public InitializeAggregatedMeasureDataProcessesHandler(
        RequestAggregatedMeasureDataValidator messageValidator,
        IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository)
    {
        _messageValidator = messageValidator;
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
    }

    public async Task<Result> Handle(
        InitializeAggregatedMeasureDataProcessesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.MarketMessage);

        var result = await _messageValidator.ValidateAsync(request.MarketMessage, cancellationToken)
            .ConfigureAwait(false);

        if (result.Errors.Count == 0)
            CreateAggregatedMeasureDataProcess(request.MarketMessage);

        return result;
    }

    private void CreateAggregatedMeasureDataProcess(
        RequestAggregatedMeasureDataMarketMessage marketMessage)
    {
        var actorSenderNumber = ActorNumber.Create(marketMessage.SenderNumber);
        var businessReason = CimCode.To(marketMessage.BusinessReason);

        foreach (var serie in marketMessage.Series)
        {
            var settlementVersion = !string.IsNullOrWhiteSpace(serie.SettlementSeriesVersion)
                ? EnumerationCodeType.FromCode<SettlementVersion>(serie.SettlementSeriesVersion)
                : null;

            _aggregatedMeasureDataProcessRepository.Add(
                new AggregatedMeasureDataProcess(
                    ProcessId.New(),
                    BusinessTransactionId.Create(serie.Id),
                    actorSenderNumber,
                    marketMessage.SenderRoleCode,
                    businessReason,
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
