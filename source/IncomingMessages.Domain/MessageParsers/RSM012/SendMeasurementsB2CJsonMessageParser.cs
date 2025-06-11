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

using System.Globalization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using NodaTime.Extensions;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM012;

public class SendMeasurementsB2CJsonMessageParser(
    ISerializer serializer)
    : B2CJsonMessageParserBase<SendMeasurementsDto>(serializer)
{
    private const string ElectricityBusinessType = "23";

    public override IncomingDocumentType DocumentType => IncomingDocumentType.B2CSendMeasurements;

    public override DocumentFormat DocumentFormat => DocumentFormat.Json;

    protected override IIncomingMessage MapIncomingMessage(SendMeasurementsDto incomingMessageDto)
    {
        // TODO: Correct pattern?
        var instantPatternWithoutSeconds = InstantPattern.CreateWithInvariantCulture("yyyy-MM-dd'T'HH:mm'Z'");

        var series = new MeteredDataForMeteringPointSeries(
            TransactionId: incomingMessageDto.TransactionId.Value,
            Resolution: incomingMessageDto.Resolution.Code,
            StartDateTime: instantPatternWithoutSeconds.Format(incomingMessageDto.Start.ToInstant()),
            EndDateTime: instantPatternWithoutSeconds.Format(incomingMessageDto.End.ToInstant()),
            ProductNumber: ProductType.EnergyActive.Code,
            RegisteredAt: instantPatternWithoutSeconds.Format(incomingMessageDto.CreatedAt.ToInstant()),
            ProductUnitType: MeasurementUnit.KilowattHour.Code,
            MeteringPointType: incomingMessageDto.MeteringPointType.Code,
            MeteringPointLocationId: incomingMessageDto.MeteringPointId.Value,
            SenderNumber: incomingMessageDto.Sender.ActorNumber.Value,
            EnergyObservations: incomingMessageDto.Measurements
                .Select(
                    m => new EnergyObservation(
                        m.Position.ToString(),
                        m.Quantity.ToString(NumberFormatInfo.InvariantInfo),
                        m.Quality.Code))
                .ToList());

        return new MeteredDataForMeteringPointMessageBase(
            messageId: incomingMessageDto.MessageId.Value,
            messageType: MessageType.ValidatedMeteredData.Code,
            createdAt: InstantPattern.General.Format(incomingMessageDto.CreatedAt.ToInstant()),
            senderNumber: incomingMessageDto.Sender.ActorNumber.Value,
            receiverNumber: DataHubDetails.DataHubActorNumber.Value,
            senderRoleCode: incomingMessageDto.Sender.ActorRole.Code,
            businessReason: BusinessReason.PeriodicMetering.Code,
            receiverRoleCode: ActorRole.MeteredDataAdministrator.Code,
            businessType: ElectricityBusinessType,
            series: [series]);
    }
}
