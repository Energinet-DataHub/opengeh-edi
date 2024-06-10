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
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.WholesaleSettlementMessageParsers;

public class WholesaleSettlementB2CJsonMessageParser : IMarketMessageParser
{
    private readonly ISerializer _serializer;

    public WholesaleSettlementB2CJsonMessageParser(
        ISerializer serializer)
    {
        _serializer = serializer;
    }

    public DocumentFormat HandledFormat => DocumentFormat.Json;

    public IncomingDocumentType DocumentType => IncomingDocumentType.B2CRequestWholesaleSettlement;

    public async Task<IncomingMarketMessageParserResult> ParseAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incomingMarketMessageStream);

        var requestWholesaleSettlementDto = await _serializer
            .DeserializeAsync<RequestWholesaleSettlementDto>(incomingMarketMessageStream.Stream, cancellationToken)
            .ConfigureAwait(false);

        var seriesCollection = requestWholesaleSettlementDto.Series
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
            requestWholesaleSettlementDto.SenderNumber,
            requestWholesaleSettlementDto.SenderRoleCode,
            requestWholesaleSettlementDto.ReceiverNumber,
            requestWholesaleSettlementDto.ReceiverRoleCode,
            requestWholesaleSettlementDto.BusinessReason,
            requestWholesaleSettlementDto.MessageType,
            requestWholesaleSettlementDto.MessageId,
            requestWholesaleSettlementDto.CreatedAt,
            requestWholesaleSettlementDto.BusinessType,
            seriesCollection);

        return new IncomingMarketMessageParserResult(requestWholesaleServicesMessage);
    }
}
