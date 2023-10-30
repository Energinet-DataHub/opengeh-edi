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
using System.Collections.ObjectModel;
using System.Linq;
using Energinet.DataHub.EDI.Application.IncomingMessages;
using NodaTime;

namespace Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;

public static class RequestAggregatedMeasureDataMarketMessageFactory
{
    public static RequestAggregatedMeasureDataMarketMessage Create(
        MessageHeader header,
        ReadOnlyCollection<Serie> series)
    {
        if (header == null) throw new ArgumentNullException(nameof(header));
        if (series == null) throw new ArgumentNullException(nameof(series));

        return new RequestAggregatedMeasureDataMarketMessage(
            header.SenderId,
            header.SenderRole,
            header.ReceiverId,
            header.ReceiverRole,
            header.BusinessReason,
            header.AuthenticatedUser,
            header.AuthenticatedUserRole,
            header.MessageType,
            header.MessageId,
            header.CreatedAt,
            header.BusinessType,
            series);
    }

    public static RequestAggregatedMeasureDataMarketMessage Create(
        Edi.Requests.RequestAggregatedMeasureData requestAggregatedMeasureData,
        Instant createdAt)
    {
        if (requestAggregatedMeasureData == null) throw new ArgumentNullException(nameof(requestAggregatedMeasureData));

        var series = requestAggregatedMeasureData.Series
            .Select(serie => new Serie(
                serie.Id,
                string.IsNullOrWhiteSpace(serie.MarketEvaluationPointType) ? null : serie.MarketEvaluationPointType,
                string.IsNullOrWhiteSpace(serie.MarketEvaluationSettlementMethod) ? null : serie.MarketEvaluationSettlementMethod,
                serie.StartDateAndOrTimeDateTime,
                string.IsNullOrWhiteSpace(serie.EndDateAndOrTimeDateTime) ? null : serie.EndDateAndOrTimeDateTime,
                string.IsNullOrWhiteSpace(serie.MeteringGridAreaDomainId) ? null : serie.MeteringGridAreaDomainId,
                string.IsNullOrWhiteSpace(serie.EnergySupplierMarketParticipantId) ? null : serie.EnergySupplierMarketParticipantId,
                string.IsNullOrWhiteSpace(serie.BalanceResponsiblePartyMarketParticipantId) ? null : serie.BalanceResponsiblePartyMarketParticipantId,
                string.IsNullOrWhiteSpace(serie.SettlementSeriesVersion) ? null : serie.SettlementSeriesVersion)).ToList();

        return new RequestAggregatedMeasureDataMarketMessage(
            requestAggregatedMeasureData.SenderId,
            requestAggregatedMeasureData.SenderRoleCode,
            requestAggregatedMeasureData.ReceiverId,
            requestAggregatedMeasureData.ReceiverRoleCode,
            requestAggregatedMeasureData.BusinessReason,
            requestAggregatedMeasureData.AuthenticatedUser,
            requestAggregatedMeasureData.AuthenticatedUserRoleCode,
            requestAggregatedMeasureData.MessageType,
            requestAggregatedMeasureData.MessageId,
            createdAt.ToString(),
            BusinessType: "23",
            series);
    }
}
