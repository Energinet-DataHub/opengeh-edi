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

using System.Xml.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Ebix;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM015;

public class RequestMeasurementsEbixMessageParser(
    EbixSchemaProvider schemaProvider,
    ILogger<RequestMeasurementsEbixMessageParser> logger)
    : EbixMessageParserBase(schemaProvider, logger)
{
    private const string SeriesElementName = "PayloadMeasuredDataRequest";
    private const string Identification = "Identification";

    private const string ObservationTimeSeriesPeriod = "ObservationTimeSeriesPeriod";
    private const string Start = "Start";
    private const string End = "End";

    private const string MeteringPointDomainLocation = "MeteringPointDomainLocation";

    public override IncomingDocumentType DocumentType => IncomingDocumentType.RequestValidatedMeasurements;

    public override DocumentFormat DocumentFormat => DocumentFormat.Json;

    protected override string RootPayloadElementName => "DK_RequestMeteredDataValidated";

    protected override IReadOnlyCollection<IIncomingMessageSeries> ParseTransactions(
        XDocument document,
        XNamespace ns,
        string senderNumber,
        string createdAt)
    {
        var transactionElements = document.Descendants(ns + SeriesElementName);
        var results = new List<RequestMeasurementsSeries>();

        foreach (var transactionElement in transactionElements)
        {
            var id = transactionElement.Element(ns + Identification)?.Value;
            var observationElement = transactionElement.Element(ns + ObservationTimeSeriesPeriod);
            var start = observationElement?.Element(ns + Start)?.Value;
            var end = observationElement?.Element(ns + End)?.Value;

            var meteringPointLocationId = transactionElement.Element(ns + MeteringPointDomainLocation)?.Element(ns + Identification)?.Value;

            results.Add(new RequestMeasurementsSeries(
                TransactionId: id ?? throw new ArgumentNullException(id, "The transaction id cannot be null."),
                StartDateTime: start ?? throw new ArgumentNullException(start, "The start cannot be null."),
                EndDateTime: end,
                MeteringPointId: MeteringPointId.From(meteringPointLocationId ?? throw new ArgumentNullException(meteringPointLocationId, "The metering point id cannot be null.")),
                SenderNumber: senderNumber));
        }

        return results.AsReadOnly();
    }

    protected override IncomingMarketMessageParserResult CreateResult(MessageHeader header, IReadOnlyCollection<IIncomingMessageSeries> transactions)
    {
        return new IncomingMarketMessageParserResult(new RequestMeasurementsMessageBase(
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
