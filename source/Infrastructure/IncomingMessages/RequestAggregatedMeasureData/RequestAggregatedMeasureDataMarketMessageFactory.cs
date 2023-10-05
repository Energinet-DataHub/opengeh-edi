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
using System.Linq;
using Energinet.DataHub.EDI.Application.IncomingMessages;
using Energinet.DataHub.EDI.Application.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Documents;
using Serie = Energinet.DataHub.EDI.Application.IncomingMessages.RequestAggregatedMeasureData.Serie;
using SerieDocument = Energinet.DataHub.EDI.Domain.Documents.Serie;

namespace Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;

public static class RequestAggregatedMeasureDataMarketMessageFactory
{
    public static RequestAggregatedMeasureDataMarketMessage Created(
        IIncomingMarketDocument<Serie, RequestAggregatedMeasureDataTransactionCommand>
            incomingMarketDocument)
    {
        if (incomingMarketDocument == null) throw new ArgumentNullException(nameof(incomingMarketDocument));

        var series = incomingMarketDocument.MarketActivityRecords
            .Select(activityRecord => new SerieDocument(
                activityRecord.Id,
                activityRecord.MarketEvaluationPointType,
                activityRecord.MarketEvaluationSettlementMethod,
                activityRecord.StartDateAndOrTimeDateTime,
                activityRecord.EndDateAndOrTimeDateTime,
                activityRecord.MeteringGridAreaDomainId,
                activityRecord.EnergySupplierMarketParticipantId,
                activityRecord.BalanceResponsiblePartyMarketParticipantId)).ToList();

        return new RequestAggregatedMeasureDataMarketMessage(
            ActorNumber.Create(incomingMarketDocument.Header.SenderId),
            MarketRole.FromCode(incomingMarketDocument.Header.SenderRole),
            ActorNumber.Create(incomingMarketDocument.Header.ReceiverId),
            MarketRole.FromCode(incomingMarketDocument.Header.ReceiverRole),
            incomingMarketDocument.Header.BusinessReason,
            incomingMarketDocument.Header.AuthenticatedUser,
            incomingMarketDocument.Header.AuthenticatedUserRole,
            incomingMarketDocument.Header.MessageType,
            incomingMarketDocument.Header.MessageId,
            series);
    }

    public static RequestAggregatedMeasureDataMarketMessage Created(Edi.Requests.RequestAggregatedMeasureData requestAggregatedMeasureData)
    {
        if (requestAggregatedMeasureData == null) throw new ArgumentNullException(nameof(requestAggregatedMeasureData));

        var series = requestAggregatedMeasureData.Series
            .Select(serie => new SerieDocument(
                serie.Id,
                serie.MarketEvaluationPointType,
                serie.MarketEvaluationSettlementMethod,
                serie.StartDateAndOrTimeDateTime,
                serie.EndDateAndOrTimeDateTime,
                serie.MeteringGridAreaDomainId,
                serie.EnergySupplierMarketParticipantId,
                serie.BalanceResponsiblePartyMarketParticipantId)).ToList();

        return new RequestAggregatedMeasureDataMarketMessage(
            ActorNumber.Create(requestAggregatedMeasureData.SenderId),
            MarketRole.FromCode(requestAggregatedMeasureData.SenderRole),
            ActorNumber.Create(requestAggregatedMeasureData.ReceiverId),
            MarketRole.FromCode(requestAggregatedMeasureData.ReceiverRole),
            requestAggregatedMeasureData.BusinessReason,
            requestAggregatedMeasureData.AuthenticatedUser,
            requestAggregatedMeasureData.AuthenticatedUserRole,
            requestAggregatedMeasureData.MessageType,
            requestAggregatedMeasureData.MessageId,
            series);
    }
}
