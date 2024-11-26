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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Json.Schema;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.WholesaleSettlementMessageParsers;

public class WholesaleSettlementB2CJsonMessageParserBase(ISerializer serializer)
    : B2CJsonMessageParserBase<RequestWholesaleSettlementDto>(serializer)
{
    public override IncomingDocumentType DocumentType => IncomingDocumentType.B2CRequestWholesaleSettlement;

    public override DocumentFormat DocumentFormat => DocumentFormat.Json;

    protected override IIncomingMessage MapIncomingMessage(RequestWholesaleSettlementDto incomingMessageDto)
    {
        var seriesCollection = incomingMessageDto.Series
            .Select(
                series => new RequestWholesaleServicesSeries(
                    series.Id,
                    series.StartDateAndOrTimeDateTime,
                    series.EndDateAndOrTimeDateTime,
                    series.MeteringGridAreaDomainId,
                    series.EnergySupplierMarketParticipantId,
                    series.SettlementVersion,
                    series.Resolution,
                    series.ChargeOwner,
                    series.ChargeTypes.Select(p => new RequestWholesaleServicesChargeType(p.Id, p.Type))
                        .ToList()
                        .AsReadOnly()))
            .ToList()
            .AsReadOnly();

        var requestWholesaleServicesMessage = new RequestWholesaleServicesMessage(
            incomingMessageDto.SenderNumber,
            incomingMessageDto.SenderRoleCode,
            incomingMessageDto.ReceiverNumber,
            incomingMessageDto.ReceiverRoleCode,
            incomingMessageDto.BusinessReason,
            incomingMessageDto.MessageType,
            incomingMessageDto.MessageId,
            incomingMessageDto.CreatedAt,
            incomingMessageDto.BusinessType,
            seriesCollection);

        return requestWholesaleServicesMessage;
    }
}
