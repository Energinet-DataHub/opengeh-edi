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

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM012;

public class MeteredDataForMeteringPointJsonMessageParser(JsonSchemaProvider schemaProvider) : JsonMessageParserBase(schemaProvider)
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
        var id = transactionElement.GetProperty(MridElementName).GetString() ?? string.Empty;
        var meteringPointLocationId = transactionElement.GetProperty(MeteringPointIdentificationElementName)
            .GetProperty(ValueElementName)
            .GetString();

        var meteringPointType = transactionElement.GetProperty(MeteringPointTypeElementName)
            .GetProperty(ValueElementName)
            .GetString();

        var registrationDateAndOrTime = transactionElement.GetProperty(RegistrationDateAndOrTimeElementName).GetString();
        var productNumber = transactionElement.GetProperty(ProductNumberElementName).GetString();
        var productUnitType = transactionElement.GetProperty(ProductUnitTypeElementName)
            .GetProperty(ValueElementName)
            .GetString();

        var periodElement = transactionElement.GetProperty(PeriodElementName);
        var resolution = periodElement.GetProperty(ResolutionElementName).GetString();
        var timeInterval = periodElement.GetProperty(TimeIntervalElementName);
        var startDateAndOrTimeDateTime = timeInterval.GetProperty(StartElementName)
            .GetProperty(ValueElementName)
            .GetString();
        var endDateAndOrTimeDateTime = timeInterval.GetProperty(EndElementName)
            .GetProperty(ValueElementName)
            .GetString();

        List<EnergyObservation>? energyObservations = null;
        if (periodElement.TryGetProperty(PointElementName, out var pointsElement))
        {
            energyObservations = new List<EnergyObservation>(pointsElement.GetArrayLength());
            foreach (var pointElement in pointsElement.EnumerateArray())
            {
                var position = pointElement.GetProperty(PositionElementName)
                    .GetProperty(ValueElementName)
                    .ToString();
                var quality = pointElement.TryGetProperty(QualityElementName, out var qualityElement)
                    ? qualityElement.GetProperty(ValueElementName).GetString()
                    : null;
                var quantity = pointElement.TryGetProperty(QuantityElementName, out var quantityElement)
                    ? quantityElement.ToString()
                    : null;

                energyObservations.Add(new EnergyObservation(position, quantity, quality));
            }
        }

        return new MeteredDataForMeteringPointSeries(
            id,
            resolution,
            startDateAndOrTimeDateTime!,
            endDateAndOrTimeDateTime,
            productNumber,
            registrationDateAndOrTime!,
            productUnitType,
            meteringPointType,
            meteringPointLocationId,
            senderNumber,
            energyObservations ?? new List<EnergyObservation>());
    }

    protected override IncomingMarketMessageParserResult CreateResult(MessageHeader header, IReadOnlyCollection<IIncomingMessageSeries> transactions)
    {
        return new IncomingMarketMessageParserResult(new MeteredDataForMeteringPointMessageBase(
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
