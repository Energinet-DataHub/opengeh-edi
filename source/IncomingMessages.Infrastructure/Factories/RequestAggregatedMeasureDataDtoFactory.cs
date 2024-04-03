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

using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.Process.Interfaces;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Factories;

public static class RequestAggregatedMeasureDataDtoFactory
{
    public static RequestAggregatedMeasureDataDto Create(RequestAggregatedMeasureDataMessage requestAggregatedMeasureDataMessage)
    {
        ArgumentNullException.ThrowIfNull(requestAggregatedMeasureDataMessage);

        var series = requestAggregatedMeasureDataMessage.Serie
            .Cast<RequestAggregatedMeasureDataSerie>()
            .Select(
                serie => new Serie(
                    serie.TransactionId,
                    serie.MarketEvaluationPointType,
                    serie.MarketEvaluationSettlementMethod,
                    serie.StartDateTime,
                    serie.EndDateTime,
                    serie.MeteringGridAreaDomainId,
                    serie.EnergySupplierMarketParticipantId,
                    serie.BalanceResponsiblePartyMarketParticipantId,
                    serie.SettlementVersion)).ToList().AsReadOnly();

        return new RequestAggregatedMeasureDataDto(
                requestAggregatedMeasureDataMessage.SenderNumber,
                requestAggregatedMeasureDataMessage.SenderRoleCode,
                requestAggregatedMeasureDataMessage.ReceiverNumber,
                requestAggregatedMeasureDataMessage.ReceiverRoleCode,
                requestAggregatedMeasureDataMessage.BusinessReason,
                requestAggregatedMeasureDataMessage.MessageType,
                requestAggregatedMeasureDataMessage.MessageId,
                requestAggregatedMeasureDataMessage.CreatedAt,
                requestAggregatedMeasureDataMessage.BusinessType,
                series);
    }
}
