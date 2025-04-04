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

using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Components.Abstractions.BusinessValidation;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using NodaTime;
using BusinessReason = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.BusinessReason;
using MeasurementUnit = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.MeasurementUnit;
using MeteringPointType = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.MeteringPointType;
using Quality = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Quality;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers.MessageFactories;

public class EnqueueBrs021ForwardMeteredDataFactory
{
    public static ServiceBusMessage CreateAcceptedV1(
        Actor actor,
        Instant start,
        Instant end,
        string originalActorMessageId,
        Guid eventId)
    {
        var resolution = BuildingBlocks.Domain.Models.Resolution.QuarterHourly;
        var resolutionDuration = resolution.ToDuration();

        var meteredData = new List<ReceiversWithMeteredDataV1.AcceptedMeteredData>();
        var currentPosition = 1;
        var currentTime = start;
        while (currentTime < end)
        {
            meteredData.Add(new ReceiversWithMeteredDataV1.AcceptedMeteredData(
                Position: currentPosition,
                EnergyQuantity: 1.04m,
                QuantityQuality: Quality.Calculated));

            currentPosition++;
            currentTime = currentTime.Plus(resolutionDuration);
        }

        var receiversWithMeteredData = new ReceiversWithMeteredDataV1(
            Actors:
            [
                new MarketActorRecipientV1(
                    actor.ActorNumber.ToProcessManagerActorNumber(),
                    actor.ActorRole.ToProcessManagerActorRole())
            ],
            Resolution: resolution.ToProcessManagerResolution(),
            MeasurementUnit.KilowattHour,
            StartDateTime: start.ToDateTimeOffset(),
            EndDateTime: end.ToDateTimeOffset(),
            MeteredData: meteredData);

        var accepted = new ForwardMeteredDataAcceptedV1(
            OriginalActorMessageId: originalActorMessageId,
            MeteringPointId: "1234567890123",
            MeteringPointType: MeteringPointType.Consumption,
            ProductNumber: "test-product-number",
            RegistrationDateTime: start.ToDateTimeOffset(),
            StartDateTime: start.ToDateTimeOffset(),
            EndDateTime: end.ToDateTimeOffset(),
            ReceiversWithMeteredData: [receiversWithMeteredData]);

        return CreateServiceBusMessage(accepted, actor, eventId);
    }

    public static ServiceBusMessage CreateRejectedV1(
        Actor actor,
        string originalActorMessageId,
        Guid eventId,
        string validationError)
    {
        var rejected = new ForwardMeteredDataRejectedV1(
            OriginalActorMessageId: originalActorMessageId,
            OriginalTransactionId: Guid.NewGuid().ToString(),
            ForwardedByActorNumber: actor.ActorNumber.ToProcessManagerActorNumber(),
            ForwardedByActorRole: actor.ActorRole.ToProcessManagerActorRole(),
            ForwardedForActorRole: actor.ActorRole.ToProcessManagerActorRole(),
            BusinessReason: BusinessReason.PeriodicMetering,
            ValidationErrors:
            [
                new ValidationErrorDto(validationError, "XYZ"),
            ]);

        return CreateServiceBusMessage(rejected, actor, eventId);
    }

    private static ServiceBusMessage CreateServiceBusMessage<TData>(
        TData data,
        Actor actor,
        Guid eventId)
        where TData : IEnqueueDataDto
    {
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_021_ForwardedMeteredData.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = actor.ActorNumber.Value,
                ActorRole = actor.ActorRole.ToProcessManagerActorRole().ToActorRoleV1(),
            },
            OrchestrationInstanceId = Guid.NewGuid().ToString(),
        };

        enqueueActorMessages.SetData(data);

        return enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: eventId.ToString());
    }
}
