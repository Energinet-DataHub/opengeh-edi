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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.AggregatedMeasureDataRequestMessageParsers;

public class AggregatedMeasureDataB2CJsonMessageParserBase(ISerializer serializer)
    : B2CJsonMessageParserBase<RequestAggregatedMeasureDataDto>(serializer)
{
    public override IncomingDocumentType DocumentType => IncomingDocumentType.B2CRequestAggregatedMeasureData;

    public override DocumentFormat DocumentFormat => DocumentFormat.Json;

    protected override IIncomingMessage MapIncomingMessage(RequestAggregatedMeasureDataDto incomingMessageDto)
    {
        var series = incomingMessageDto.Serie
            .Select(
                serie => new RequestAggregatedMeasureDataMessageSeries(
                    serie.Id,
                    serie.MarketEvaluationPointType,
                    serie.MarketEvaluationSettlementMethod,
                    serie.StartDateAndOrTimeDateTime,
                    serie.EndDateAndOrTimeDateTime,
                    serie.MeteringGridAreaDomainId,
                    serie.EnergySupplierMarketParticipantId,
                    serie.BalanceResponsiblePartyMarketParticipantId,
                    serie.SettlementVersion)).ToList().AsReadOnly();

        var requestAggregatedMeasureData = new RequestAggregatedMeasureDataMessage(
            incomingMessageDto.SenderNumber,
            incomingMessageDto.SenderRoleCode,
            incomingMessageDto.ReceiverNumber,
            incomingMessageDto.ReceiverRoleCode,
            incomingMessageDto.BusinessReason,
            incomingMessageDto.MessageType,
            incomingMessageDto.MessageId,
            incomingMessageDto.CreatedAt,
            incomingMessageDto.BusinessType,
            series);

        return requestAggregatedMeasureData;
    }
}
