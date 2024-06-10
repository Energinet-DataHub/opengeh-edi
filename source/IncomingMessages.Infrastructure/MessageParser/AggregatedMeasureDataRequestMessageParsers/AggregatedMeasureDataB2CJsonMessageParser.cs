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
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.AggregatedMeasureDataRequestMessageParsers;

public class AggregatedMeasureDataB2CJsonMessageParser : IMarketMessageParser
{
    private readonly ISerializer _serializer;

    public AggregatedMeasureDataB2CJsonMessageParser(
        ISerializer serializer)
        : base()
    {
        _serializer = serializer;
    }

    public DocumentFormat HandledFormat => DocumentFormat.Json;

    public IncomingDocumentType DocumentType => IncomingDocumentType.B2CRequestAggregatedMeasureData;

    public async Task<IncomingMarketMessageParserResult> ParseAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incomingMarketMessageStream);

        var requestAggregatedMeasureDataDto = await _serializer
            .DeserializeAsync<RequestAggregatedMeasureDataDto>(incomingMarketMessageStream.Stream, cancellationToken)
            .ConfigureAwait(false);

        var series = requestAggregatedMeasureDataDto.Serie
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
            requestAggregatedMeasureDataDto.SenderNumber,
            requestAggregatedMeasureDataDto.SenderRoleCode,
            requestAggregatedMeasureDataDto.ReceiverNumber,
            requestAggregatedMeasureDataDto.ReceiverRoleCode,
            requestAggregatedMeasureDataDto.BusinessReason,
            requestAggregatedMeasureDataDto.MessageType,
            requestAggregatedMeasureDataDto.MessageId,
            requestAggregatedMeasureDataDto.CreatedAt,
            requestAggregatedMeasureDataDto.BusinessType,
            series);

        return new IncomingMarketMessageParserResult(requestAggregatedMeasureData);
    }
}
