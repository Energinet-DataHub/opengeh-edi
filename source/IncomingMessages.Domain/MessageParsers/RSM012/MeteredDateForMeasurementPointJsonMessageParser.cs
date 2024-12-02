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

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM012;

public class MeteredDateForMeasurementPointJsonMessageParser(JsonSchemaProvider schemaProvider) : JsonMessageParserBase(schemaProvider)
{
    private const string ValueElementName = "value";
    private const string MridElementName = "mRID";
    private const string MeteringPointIdentificationElementName = "marketEvaluationPoint.mRID";
    private const string MeteringPointTypeElementName = "marketEvaluationPoint.type";
    private const string ProductNumberElementName = "product";
    private const string ProductUnitTypeElementName = "quantity_Measure_Unit.name";
    private const string PeriodElementName = "Period";
    private const string ResolutionElementName = "resolution";
    private const string TimeIntervalElementName = "timeInterval";
    private const string StartElementName = "start";
    private const string EndElementName = "end";
    private const string PointElementName = "Point";
    private const string PositionElementName = "position";
    private const string QualityElementName = "quality";
    private const string QuantityElementName = "quantity";
    private const string RegistrationDateAndOrTimeElementName = "registration_DateAndOrTime.dateTime";

    public override IncomingDocumentType DocumentType => IncomingDocumentType.NotifyValidatedMeasureData;

    public override DocumentFormat DocumentFormat => DocumentFormat.Json;

    protected override string HeaderElementName => "NotifyValidatedMeasureData_MarketDocument";

    protected override string DocumentName => "NotifyValidatedMeasureData";

    protected override IIncomingMessageSeries ParseTransaction(JsonElement transactionElement, string senderNumber)
    {
        var id = transactionElement.GetProperty(MridElementName).ToString() ?? string.Empty;
        var meteringPointLocationId = transactionElement.GetProperty(MeteringPointIdentificationElementName)
            .GetProperty(ValueElementName)
            .ToString();
        var meteringPointType =
            transactionElement.GetProperty(MeteringPointTypeElementName).GetProperty(ValueElementName).ToString();
        var registrationDateAndOrTime =
            transactionElement.GetProperty(RegistrationDateAndOrTimeElementName).ToString();
        var productNumber =
            transactionElement.GetProperty(ProductNumberElementName).ToString();
        var productUnitType =
            transactionElement.GetProperty(ProductUnitTypeElementName).GetProperty(ValueElementName).ToString();
        var periodElement = transactionElement.GetProperty(PeriodElementName);
        var resolution = periodElement.GetProperty(ResolutionElementName).ToString();
        var startDateAndOrTimeDateTime = periodElement.GetProperty(TimeIntervalElementName).GetProperty(StartElementName).GetProperty(ValueElementName).ToString();
        var endDateAndOrTimeDateTime = periodElement.GetProperty(TimeIntervalElementName).GetProperty(EndElementName).GetProperty(ValueElementName).ToString();

        var energyObservations = new List<EnergyObservation>();
        JsonElement? pointsElements = transactionElement.TryGetProperty(PointElementName, out var pointsElement)
            ? pointsElement
            : null;
        if (pointsElements != null)
        {
            foreach (var pointElement in pointsElements.Value.EnumerateArray())
            {
                energyObservations.Add(new EnergyObservation(
                    pointElement.GetProperty(PositionElementName).GetProperty(ValueElementName).ToString(),
                    pointElement.GetProperty(QualityElementName).GetProperty(ValueElementName).ToString(),
                    pointElement.GetProperty(QuantityElementName).ToString()));
            }
        }

        return new MeteredDataForMeasurementPointSeries(
            id,
            resolution,
            startDateAndOrTimeDateTime,
            endDateAndOrTimeDateTime,
            productNumber,
            registrationDateAndOrTime,
            productUnitType,
            meteringPointType,
            meteringPointLocationId,
            senderNumber,
            energyObservations);
    }

    protected override IncomingMarketMessageParserResult CreateResult(MessageHeader header, IReadOnlyCollection<IIncomingMessageSeries> transactions)
    {
        return new IncomingMarketMessageParserResult(new MeteredDataForMeasurementPointMessage(
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
