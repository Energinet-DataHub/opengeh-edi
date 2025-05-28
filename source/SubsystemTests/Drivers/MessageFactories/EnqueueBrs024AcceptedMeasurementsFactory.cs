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
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_024;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_024.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using NodaTime;
using PMValueTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers.MessageFactories;

public class EnqueueBrs024AcceptedMeasurementsFactory
{
    public static ServiceBusMessage CreateAcceptedV1(
        Actor actor,
        Instant start,
        Instant end,
        string originalActorMessageId,
        string originalActorTransactionId,
        Guid eventId)
    {
        var resolution = PMValueTypes.Resolution.QuarterHourly;
        var accepted = new RequestYearlyMeasurementsAcceptedV1(
            OriginalActorMessageId: originalActorMessageId,
            OriginalTransactionId: originalActorTransactionId,
            MeteringPointId: "123456789012345678",
            MeteringPointType: PMValueTypes.MeteringPointType.Consumption,
            ProductNumber: "test-product-number",
            RegistrationDateTime: end.ToDateTimeOffset(),
            StartDateTime: start.ToDateTimeOffset(),
            EndDateTime: end.ToDateTimeOffset(),
            ActorNumber: actor.ActorNumber.ToProcessManagerActorNumber(),
            ActorRole: actor.ActorRole.ToProcessManagerActorRole(),
            Resolution: resolution,
            MeasureUnit: PMValueTypes.MeasurementUnit.KilowattHour,
            Measurements: GetMeasurements(start, end, resolution),
            GridAreaCode: "804");

        return CreateServiceBusMessage(accepted, actor, eventId);
    }

    private static ServiceBusMessage CreateServiceBusMessage<TData>(
        TData data,
        Actor actor,
        Guid eventId)
        where TData : IEnqueueDataDto
    {
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_024.Name,
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

    private static IReadOnlyCollection<AcceptedMeteredData> GetMeasurements(
        Instant startDateTime,
        Instant endDateTime,
        PMValueTypes.Resolution resolution)
    {
        var measurements = new List<AcceptedMeteredData>();
        var interval = resolution switch
        {
            var res when res == PMValueTypes.Resolution.QuarterHourly => Duration.FromMinutes(15),
            var res when res == PMValueTypes.Resolution.Hourly => Duration.FromHours(1),
            var res when res == PMValueTypes.Resolution.Daily => Duration.FromDays(1),
            _ => throw new ArgumentOutOfRangeException(nameof(resolution), "Unsupported resolution"),
        };

        var position = 1;
        for (var timestamp = startDateTime; timestamp < endDateTime; timestamp += interval)
        {
            measurements.Add(
                new AcceptedMeteredData(
                    Position: position,
                    EnergyQuantity: GenerateRandomMeasurementValue(),
                    QuantityQuality: PMValueTypes.Quality.AsProvided));
            position++;
        }

        return measurements;
    }

    private static decimal GenerateRandomMeasurementValue()
    {
        // Example: Generate a random value for demonstration purposes
        var random = new Random();
        return (decimal)(random.Next(0, 10000) / 100.0); // Random decimal value between 0.00 and 100.00
    }
}
