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

using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Cim.Json;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM016;

public class AggregatedMeasureDataJsonMessageParser(JsonSchemaProvider schemaProvider) : JsonMessageParserBase(schemaProvider)
{
    private const string MridElementName = "mRID";
    private const string MarketEvaluationPointTypeElementName = "marketEvaluationPoint.type";
    private const string MarketEvaluationPointSettlementMethodElementName = "marketEvaluationPoint.settlementMethod";
    private const string StartElementName = "start_DateAndOrTime.dateTime";
    private const string EndElementName = "end_DateAndOrTime.dateTime";
    private const string GridAreaElementName = "meteringGridArea_Domain.mRID";
    private const string EnergySupplierNumberElementName = "energySupplier_MarketParticipant.mRID";
    private const string BalanceResponsibleNumberElementName = "balanceResponsibleParty_MarketParticipant.mRID";
    private const string SettlementVersionElementName = "settlement_Series.version";

    public override IncomingDocumentType DocumentType => IncomingDocumentType.RequestAggregatedMeasureData;

    public override DocumentFormat DocumentFormat => DocumentFormat.Json;

    protected override string HeaderElementName => "RequestAggregatedMeasureData_MarketDocument";

    protected override string DocumentName => "RequestAggregatedMeasureData";

    protected override IIncomingMessageSeries ParseTransaction(JsonElement transactionElement, string senderNumber)
    {
        var id = transactionElement.GetProperty(MridElementName).ToString();
        var startDateTime = transactionElement.GetProperty(StartElementName).ToString();
        var endDateTime = transactionElement.TryGetProperty(EndElementName, out var endDateProperty) ? endDateProperty.ToString() : null;

        var meteringPointType = GetPropertyWithValue(transactionElement, MarketEvaluationPointTypeElementName);
        var settlementMethod = GetPropertyWithValue(transactionElement, MarketEvaluationPointSettlementMethodElementName);
        var gridArea = GetPropertyWithValue(transactionElement, GridAreaElementName);
        var energySupplierId = GetPropertyWithValue(transactionElement, EnergySupplierNumberElementName);
        var balanceResponsibleId = GetPropertyWithValue(transactionElement, BalanceResponsibleNumberElementName);
        var settlementVersion = GetPropertyWithValue(transactionElement, SettlementVersionElementName);

        return new RequestAggregatedMeasureDataMessageSeries(
            id,
            meteringPointType,
            settlementMethod,
            startDateTime,
            endDateTime,
            gridArea,
            energySupplierId,
            balanceResponsibleId,
            settlementVersion);
    }

    protected override IncomingMarketMessageParserResult CreateResult(MessageHeader header, IReadOnlyCollection<IIncomingMessageSeries> transactions)
    {
        return new IncomingMarketMessageParserResult(
            new RequestAggregatedMeasureDataMessage(
                header.SenderId,
                header.SenderRole,
                header.ReceiverId,
                header.ReceiverRole,
                header.BusinessReason,
                header.MessageType,
                header.MessageId,
                header.CreatedAt,
                header.BusinessType,
                transactions));
    }

    private static string? GetPropertyWithValue(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) ? property.GetProperty("value").ToString() : null;
    }
}
