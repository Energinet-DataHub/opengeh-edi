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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Cim.Json;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM015;

public class RequestValidatedMeasurementsJsonMessageParser(
    JsonSchemaProvider schemaProvider,
    ILogger<RequestValidatedMeasurementsJsonMessageParser> logger)
    : JsonMessageParserBase(schemaProvider, logger)
{
    private const string ValueElementName = "value";
    private const string MridElementName = "mRID";
    private const string EndElementName = "end_DateAndOrTime.dateTime";
    private const string StartElementName = "start_DateAndOrTime.dateTime";
    private const string MeteringPointIdentificationElementName = "marketEvaluationPoint.mRID";

    public override IncomingDocumentType DocumentType => IncomingDocumentType.RequestValidatedMeasurements;

    public override DocumentFormat DocumentFormat => DocumentFormat.Json;

    protected override string HeaderElementName => "RequestValidatedMeasureData_MarketDocument";

    protected override string DocumentName => "RequestValidatedMeasureData";

    protected override IIncomingMessageSeries ParseTransaction(JsonElement transactionElement, string senderNumber)
    {
        var id = transactionElement.GetProperty(MridElementName).GetString() ?? string.Empty;
        var meteringPointLocationId = transactionElement.GetProperty(MeteringPointIdentificationElementName)
            .GetProperty(ValueElementName)
            .GetString();

        var startDateAndOrTimeDateTime = transactionElement.GetProperty(StartElementName)
            .GetString();
        var endDateAndOrTimeDateTime = transactionElement.GetProperty(EndElementName)
            .GetString();

        return new RequestValidatedMeasurementsSeries(
            id,
            startDateAndOrTimeDateTime!,
            endDateAndOrTimeDateTime,
            meteringPointLocationId,
            senderNumber);
    }

    protected override IncomingMarketMessageParserResult CreateResult(MessageHeader header, IReadOnlyCollection<IIncomingMessageSeries> transactions)
    {
        return new IncomingMarketMessageParserResult(new RequestValidatedMeasurementsMessageBase(
            header.MessageId,
            header.MessageType,
            header.CreatedAt,
            header.SenderId,
            header.ReceiverId,
            header.SenderRole,
            header.BusinessReason,
            header.ReceiverRole,
            header.BusinessType,
            transactions));
    }
}
