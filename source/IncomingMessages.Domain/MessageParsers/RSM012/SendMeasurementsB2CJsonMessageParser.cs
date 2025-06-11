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
using NodaTime.Text;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM012;

public class SendMeasurementsB2CJsonMessageParser(
    ISerializer serializer)
    : B2CJsonMessageParserBase<SendMeasurementsDto>(serializer)
{
    public override IncomingDocumentType DocumentType => IncomingDocumentType.B2CSendMeasurements;

    public override DocumentFormat DocumentFormat => DocumentFormat.Json;

    protected override IIncomingMessage MapIncomingMessage(SendMeasurementsDto incomingMessageDto)
    {
        var series = new MeteredDataForMeteringPointSeries(
            TransactionId: incomingMessageDto.TransactionId.Value,
            Resolution: incomingMessageDto.Resolution.Code,
            StartDateTime: InstantPattern.General.Format(incomingMessageDto.Start),
            EndDateTime: InstantPattern.General.Format(incomingMessageDto.End),
            ProductNumber: ProductType.EnergyActive.Code, // TODO: Correct product number
            RegisteredAt: InstantPattern.General.Format(incomingMessageDto.CreatedAt),
            ProductUnitType: MeasurementUnit.KilowattHour.Code, // TODO: Correct product unit type?
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
            messageType: BuildingBlocks.Domain.Models.DocumentType.NotifyValidatedMeasureData.Name, // TODO: Correct message type
            createdAt: InstantPattern.General.Format(incomingMessageDto.CreatedAt),
            senderNumber: incomingMessageDto.Sender.ActorNumber.Value,
            receiverNumber: DataHubDetails.DataHubActorNumber.Value,
            senderRoleCode: incomingMessageDto.Sender.ActorRole.Code,
            businessReason: BusinessReason.PeriodicMetering.Code,
            receiverRoleCode: ActorRole.SystemOperator.Code, // TODO: Correct receiver role code?
            businessType: "A", // TODO: Correct business type?
            series: [series]);
    }
}
