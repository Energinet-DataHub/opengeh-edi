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
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using NodaTime.Text;
using static Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model.ForwardMeteredDataAcceptedV1;
using PMActorNumber = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorNumber;
using PMActorRole = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorRole;
using PMMeasureUnit = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.MeasurementUnit;
using PMMeteringPointType = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.MeteringPointType;
using PMQuality = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Quality;
using PMResolution = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

public static class MeteredDataForMeteringPointEventBuilder
{
    public static ServiceBusMessage GenerateAcceptedFrom(
        ForwardMeteredDataInputV1 requestMeteredDataForMeteringPointMessageInputV1,
        (ActorNumber ActorNumber, ActorRole ActorRole) receiverActor)
    {
        var invariantPattern = InstantPattern.CreateWithInvariantCulture("yyyy-MM-dd'T'HH':'mm'Z'");

        var meteringPointId = requestMeteredDataForMeteringPointMessageInputV1.MeteringPointId
            ?? throw new ArgumentNullException(nameof(requestMeteredDataForMeteringPointMessageInputV1.MeteringPointId), "MeteringPointId must be set");

        var meteringPointType = requestMeteredDataForMeteringPointMessageInputV1.MeteringPointType != null
            ? PMMeteringPointType.FromName(requestMeteredDataForMeteringPointMessageInputV1.MeteringPointType)
            : throw new ArgumentNullException(nameof(requestMeteredDataForMeteringPointMessageInputV1.MeteringPointType), "MeteringPointType must be set");

        var productNumber = requestMeteredDataForMeteringPointMessageInputV1.ProductNumber
            ?? throw new ArgumentNullException(nameof(requestMeteredDataForMeteringPointMessageInputV1.ProductNumber), "ProductNumber must be set");

        var measureUnit = requestMeteredDataForMeteringPointMessageInputV1.MeasureUnit != null
            ? PMMeasureUnit.FromName(requestMeteredDataForMeteringPointMessageInputV1.MeasureUnit)
            : throw new ArgumentNullException(nameof(requestMeteredDataForMeteringPointMessageInputV1.MeasureUnit), "MeasureUnit must be set");

        var resolution = requestMeteredDataForMeteringPointMessageInputV1.Resolution != null
            ? PMResolution.FromName(requestMeteredDataForMeteringPointMessageInputV1.Resolution)
            : throw new ArgumentNullException(nameof(requestMeteredDataForMeteringPointMessageInputV1.Resolution), "Resolution must be set");

        var endDateTime = requestMeteredDataForMeteringPointMessageInputV1.EndDateTime != null
            ? invariantPattern.Parse(requestMeteredDataForMeteringPointMessageInputV1.EndDateTime).Value.ToDateTimeOffset()
            : throw new ArgumentNullException(nameof(requestMeteredDataForMeteringPointMessageInputV1), "EndDateTime must be set");

        var acceptedEnergyObservations = requestMeteredDataForMeteringPointMessageInputV1.EnergyObservations
            .Select(eo => new AcceptedEnergyObservation(
                int.Parse(eo.Position!),
                eo.EnergyQuantity != null ? decimal.Parse(eo.EnergyQuantity.TrimEnd('M'), CultureInfo.InvariantCulture) : null,
                eo.QuantityQuality != null ? PMQuality.FromName(Quality.FromCode(eo.QuantityQuality).Name) : null))
            .ToList();

        var marketActorRecipients = new List<MarketActorRecipientV1>
        {
            new(
                PMActorNumber.Create(receiverActor.ActorNumber.Value),
                PMActorRole.FromName(receiverActor.ActorRole.Name)),
        };

        var acceptRequest = new ForwardMeteredDataAcceptedV1(
            OriginalActorMessageId: requestMeteredDataForMeteringPointMessageInputV1.MessageId,
            MeteringPointId: requestMeteredDataForMeteringPointMessageInputV1.MeteringPointId,
            MeteringPointType: meteringPointType,
            OriginalTransactionId: requestMeteredDataForMeteringPointMessageInputV1.TransactionId,
            ProductNumber: requestMeteredDataForMeteringPointMessageInputV1.ProductNumber,
            MeasureUnit: measureUnit,
            RegistrationDateTime: InstantPattern.General.Parse(requestMeteredDataForMeteringPointMessageInputV1.RegistrationDateTime).Value.ToDateTimeOffset(),
            Resolution: resolution,
            StartDateTime: invariantPattern.Parse(requestMeteredDataForMeteringPointMessageInputV1.StartDateTime).Value.ToDateTimeOffset(),
            EndDateTime: endDateTime,
            AcceptedEnergyObservations: acceptedEnergyObservations,
            MarketActorRecipients: marketActorRecipients);

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_021_ForwardedMeteredData.Name,
            OrchestrationVersion = Brs_021_ForwardedMeteredData.V1.Version,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = requestMeteredDataForMeteringPointMessageInputV1.ActorNumber,
                ActorRole = PMActorRole.FromName(requestMeteredDataForMeteringPointMessageInputV1.ActorRole).ToActorRoleV1(),
            },
            OrchestrationInstanceId = Guid.NewGuid().ToString(),
        };
        enqueueActorMessages.SetData(acceptRequest);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_021_ForwardedMeteredData.Name),
            idempotencyKey: Guid.NewGuid().ToString());

        return serviceBusMessage;
    }
}
