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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Cim.Xml;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM015;

public class RequestValidatedMeasurementsXmlMessageParser(CimXmlSchemaProvider schemaProvider)
    : XmlMessageParserBase(schemaProvider)
{
    public override IncomingDocumentType DocumentType => IncomingDocumentType.RequestValidatedMeasurements;

    protected override string RootPayloadElementName => "RequestValidatedMeasureData_MarketDocument";

    protected override IIncomingMessageSeries ParseTransaction(
        XElement seriesElement,
        XNamespace ns,
        string senderNumber) =>
        new RequestMeasurementsSeries(
            seriesElement.Element(ns + "mRID")!.Value,
            seriesElement.Element(ns + "start_DateAndOrTime.dateTime")!.Value,
            seriesElement.Element(ns + "end_DateAndOrTime.dateTime")?.Value,
            MeteringPointId.From(seriesElement.Element(ns + "marketEvaluationPoint.mRID")!.Value),
            senderNumber);

    protected override IncomingMarketMessageParserResult CreateResult(
        MessageHeader header,
        IReadOnlyCollection<IIncomingMessageSeries> transactions) => new(
        new RequestMeasurementsMessageBase(
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
