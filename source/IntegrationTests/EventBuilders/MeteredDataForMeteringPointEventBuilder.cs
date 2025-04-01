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
using Energinet.DataHub.ProcessManager.Components.Abstractions.BusinessValidation;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using NodaTime.Text;
using PMActorNumber = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorNumber;
using PMActorRole = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorRole;
using PMBusinessReason = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.BusinessReason;
using PMMeasureUnit = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.MeasurementUnit;
using PMMeteringPointType = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.MeteringPointType;
using PMQuality = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Quality;
using PMResolution = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

public static class MeteredDataForMeteringPointEventBuilder
{
    public static ServiceBusMessage GenerateAcceptedFrom(
        ForwardMeteredDataInputV1 requestMeteredDataForMeteringPointMessageInputV1,
        (ActorNumber ActorNumber, ActorRole ActorRole) receiverActor,
        Guid orchestrationInstanceId,
        DocumentFormat documentFormat)
    {
        var invariantPattern = InstantPattern.CreateWithInvariantCulture("yyyy-MM-dd'T'HH':'mm'Z'");

        if (documentFormat == DocumentFormat.Ebix)
        {
            invariantPattern = InstantPattern.CreateWithInvariantCulture("yyyy-MM-dd'T'HH':'mm':'ss'Z'");
        }

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

        var startDateTime = invariantPattern.Parse(requestMeteredDataForMeteringPointMessageInputV1.StartDateTime).Value.ToDateTimeOffset();

        var endDateTime = requestMeteredDataForMeteringPointMessageInputV1.EndDateTime != null
            ? invariantPattern.Parse(requestMeteredDataForMeteringPointMessageInputV1.EndDateTime).Value.ToDateTimeOffset()
            : throw new ArgumentNullException(nameof(requestMeteredDataForMeteringPointMessageInputV1), "EndDateTime must be set");

        var acceptedEnergyObservations = requestMeteredDataForMeteringPointMessageInputV1.MeteredDataList
            .Select(eo => new ReceiversWithMeteredDataV1.AcceptedMeteredData(
                Position: int.Parse(eo.Position!),
                EnergyQuantity: eo.EnergyQuantity != null ? decimal.Parse(eo.EnergyQuantity.TrimEnd('M'), CultureInfo.InvariantCulture) : null,
                QuantityQuality: eo.QuantityQuality != null ? PMQuality.FromName(eo.QuantityQuality) : null))
            .ToList();

        List<ReceiversWithMeteredDataV1> receiversWithMeteredData =
        [
            new ReceiversWithMeteredDataV1(
                Actors:
                [
                    new MarketActorRecipientV1(
                        PMActorNumber.Create(receiverActor.ActorNumber.Value),
                        PMActorRole.FromName(receiverActor.ActorRole.Name)),
                ],
                Resolution: resolution,
                MeasureUnit: measureUnit,
                StartDateTime: startDateTime,
                EndDateTime: endDateTime,
                acceptedEnergyObservations),
        ];

        var acceptRequest = new ForwardMeteredDataAcceptedV1(
            OriginalActorMessageId: requestMeteredDataForMeteringPointMessageInputV1.ActorMessageId,
            MeteringPointId: meteringPointId,
            MeteringPointType: meteringPointType,
            ProductNumber: productNumber,
            RegistrationDateTime: InstantPattern.General.Parse(requestMeteredDataForMeteringPointMessageInputV1.RegistrationDateTime).Value.ToDateTimeOffset(),
            StartDateTime: startDateTime,
            EndDateTime: endDateTime,
            ReceiversWithMeteredData: receiversWithMeteredData);

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_021_ForwardedMeteredData.Name,
            OrchestrationVersion = Brs_021_ForwardedMeteredData.V1.Version,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = requestMeteredDataForMeteringPointMessageInputV1.ActorNumber,
                ActorRole = PMActorRole.FromName(requestMeteredDataForMeteringPointMessageInputV1.ActorRole).ToActorRoleV1(),
            },
            OrchestrationInstanceId = orchestrationInstanceId.ToString(),
        };
        enqueueActorMessages.SetData(acceptRequest);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_021_ForwardedMeteredData.Name),
            idempotencyKey: Guid.NewGuid().ToString());

        return serviceBusMessage;
    }

    public static ServiceBusMessage GenerateRejectedFrom(ForwardMeteredDataInputV1 requestMeteredDataForMeteringPointInputV1, Guid orchestrationInstanceId)
    {
        var rejectRequest = new ForwardMeteredDataRejectedV1(
            requestMeteredDataForMeteringPointInputV1.ActorMessageId,
            requestMeteredDataForMeteringPointInputV1.TransactionId,
            PMActorNumber.Create(requestMeteredDataForMeteringPointInputV1.ActorNumber),
            PMActorRole.FromName(requestMeteredDataForMeteringPointInputV1.ActorRole),
            PMBusinessReason.FromName(requestMeteredDataForMeteringPointInputV1.BusinessReason),
            new List<ValidationErrorDto>()
            {
                new("Invalid Period", "E17"),
            });
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_021_ForwardedMeteredData.Name,
            OrchestrationVersion = Brs_021_ForwardedMeteredData.V1.Version,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = requestMeteredDataForMeteringPointInputV1.ActorNumber,
                ActorRole = PMActorRole.FromName(requestMeteredDataForMeteringPointInputV1.ActorRole).ToActorRoleV1(),
            },
            OrchestrationInstanceId = orchestrationInstanceId.ToString(),
        };

        enqueueActorMessages.SetData(rejectRequest);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_021_ForwardedMeteredData.Name),
            idempotencyKey: Guid.NewGuid().ToString());

        return serviceBusMessage;
    }
}
