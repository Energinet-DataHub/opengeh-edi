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
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Documents;

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
            series);
    }

    public static RequestAggregatedMeasureDataMarketMessage Create(Edi.Requests.RequestAggregatedMeasureData requestAggregatedMeasureData)
    {
        if (requestAggregatedMeasureData == null) throw new ArgumentNullException(nameof(requestAggregatedMeasureData));

        var series = requestAggregatedMeasureData.Series
            .Select(serie => new Serie(
                serie.Id,
                serie.MarketEvaluationPointType,
                serie.MarketEvaluationSettlementMethod,
                serie.StartDateAndOrTimeDateTime,
                serie.EndDateAndOrTimeDateTime,
                serie.MeteringGridAreaDomainId,
                serie.EnergySupplierMarketParticipantId,
                serie.BalanceResponsiblePartyMarketParticipantId)).ToList();

        return new RequestAggregatedMeasureDataMarketMessage(
            requestAggregatedMeasureData.SenderId,
            requestAggregatedMeasureData.SenderRole,
            requestAggregatedMeasureData.ReceiverId,
            requestAggregatedMeasureData.ReceiverRole,
            requestAggregatedMeasureData.BusinessReason,
            requestAggregatedMeasureData.AuthenticatedUser,
            requestAggregatedMeasureData.AuthenticatedUserRole,
            requestAggregatedMeasureData.MessageType,
            requestAggregatedMeasureData.MessageId,
            series);
    }
}
