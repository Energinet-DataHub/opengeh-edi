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
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_024;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_024.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using NodaTime;
using PMValueTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

public static class RequestYearlyMeasurementsResponseBuilder
{
    public static ServiceBusMessage GenerateAcceptedFrom(
        RequestYearlyMeasurementsInputV1 requestYearlyMeasurementsInputV1,
        Actor receiverActor,
        Instant startDateTime,
        Instant endDateTime,
        Guid orchestrationInstanceId,
        (int Position, string QuantityQuality, decimal EnergyQuantity) aggregatedMeasurement)
    {
        var resolution = PMValueTypes.Resolution.Yearly;
        var accepted = new RequestYearlyMeasurementsAcceptedV1(
            OriginalActorMessageId: requestYearlyMeasurementsInputV1.ActorMessageId,
            OriginalTransactionId: requestYearlyMeasurementsInputV1.TransactionId,
            MeteringPointId: requestYearlyMeasurementsInputV1.MeteringPointId,
            MeteringPointType: PMValueTypes.MeteringPointType.Consumption,
            ActorNumber: receiverActor.ActorNumber.ToProcessManagerActorNumber(),
            ActorRole: receiverActor.ActorRole.ToProcessManagerActorRole(),
            AggregatedMeasurements: new List<AggregatedMeasurement>
            {
                new(
                    StartDateTime: startDateTime.ToDateTimeOffset(),
                    EndDateTime: endDateTime.ToDateTimeOffset(),
                    Resolution: resolution,
                    EnergyQuantity: aggregatedMeasurement.EnergyQuantity,
                    QuantityQuality: PMValueTypes.Quality.AsProvided),
            });

        return CreateServiceBusMessage(receiverActor, orchestrationInstanceId, accepted);
    }

    public static ServiceBusMessage GenerateRejectedFrom(
        RequestYearlyMeasurementsInputV1 requestYearlyMeasurementsInputV1,
        Actor receiverActor,
        string validationErrorMessage,
        string validationErrorCode,
        Guid orchestrationInstanceId)
    {
        var rejected = new RequestYearlyMeasurementsRejectV1(
            OriginalActorMessageId: requestYearlyMeasurementsInputV1.ActorMessageId,
            OriginalTransactionId: requestYearlyMeasurementsInputV1.TransactionId,
            ActorNumber: receiverActor.ActorNumber.ToProcessManagerActorNumber(),
            ActorRole: receiverActor.ActorRole.ToProcessManagerActorRole(),
            MeteringPointId: requestYearlyMeasurementsInputV1.MeteringPointId,
            ValidationErrors: [new ValidationErrorDto(validationErrorMessage, validationErrorCode)]);

        return CreateServiceBusMessage(receiverActor, orchestrationInstanceId, rejected);
    }

    private static ServiceBusMessage CreateServiceBusMessage<TData>(
        Actor receiverActor,
        Guid orchestrationInstanceId,
        TData data)
        where TData : IEnqueueDataDto
    {
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_024.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = receiverActor.ActorNumber.Value,
                ActorRole = receiverActor.ActorRole.ToProcessManagerActorRole().ToActorRoleV1(),
            },
            OrchestrationInstanceId = orchestrationInstanceId.ToString(),
        };

        enqueueActorMessages.SetData(data);

        return enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: Guid.NewGuid().ToString());
    }
}
