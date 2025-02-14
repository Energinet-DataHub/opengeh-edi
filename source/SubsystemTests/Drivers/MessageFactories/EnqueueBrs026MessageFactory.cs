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
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Components.Abstractions.BusinessValidation;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using NodaTime.Text;
using ProcessManagerTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers.MessageFactories;

public static class EnqueueBrs026MessageFactory
{
    public static ServiceBusMessage CreateAccept(
        Actor actor,
        string gridArea)
    {
        var balanceResponsible = actor.ActorRole == ActorRole.BalanceResponsibleParty ? actor.ActorNumber.ToProcessManagerActorNumber() : null;
        var energySupplier = actor.ActorRole == ActorRole.EnergySupplier ? actor.ActorNumber.ToProcessManagerActorNumber() : null;

        var accepted = new RequestCalculatedEnergyTimeSeriesAcceptedV1(
            OriginalActorMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            RequestedForActorNumber: actor.ActorNumber.ToProcessManagerActorNumber(),
            RequestedForActorRole: actor.ActorRole.ToProcessManagerActorRole(),
            RequestedByActorNumber: actor.ActorNumber.ToProcessManagerActorNumber(),
            RequestedByActorRole: actor.ActorRole.ToProcessManagerActorRole(),
            BusinessReason: ProcessManagerTypes.BusinessReason.FromName(BusinessReason.BalanceFixing.Name),
            PeriodStart: InstantPattern.General.Parse("2023-01-31T23:00:00Z").GetValueOrThrow().ToDateTimeOffset(),
            PeriodEnd: InstantPattern.General.Parse("2023-02-02T23:00:00Z").GetValueOrThrow().ToDateTimeOffset(),
            GridAreas: [gridArea],
            EnergySupplierNumber: null, // We cannot set the "EnergySupplierNumber" even if the actor is an energy supplier, since no data exists for our energy supplier actor, so we must set the number to "null" to retrieve any data.
            BalanceResponsibleNumber: balanceResponsible,
            MeteringPointType: ProcessManagerTypes.MeteringPointType.Consumption,
            SettlementMethod: ProcessManagerTypes.SettlementMethod.Flex,
            SettlementVersion: null);

        return CreateServiceBusMessage(accepted, actor);
    }

    public static ServiceBusMessage CreateReject(
        Actor actor)
    {
        var rejected = new RequestCalculatedEnergyTimeSeriesRejectedV1(
            OriginalMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            RequestedForActorNumber: actor.ActorNumber.ToProcessManagerActorNumber(),
            RequestedForActorRole: actor.ActorRole.ToProcessManagerActorRole(),
            RequestedByActorNumber: actor.ActorNumber.ToProcessManagerActorNumber(),
            RequestedByActorRole: actor.ActorRole.ToProcessManagerActorRole(),
            BusinessReason: ProcessManagerTypes.BusinessReason.FromName(BusinessReason.BalanceFixing.Name),
            ValidationErrors: new List<ValidationErrorDto>()
            {
                new ValidationErrorDto(Message: "Test Rejection", ErrorCode: "888"),
            });

        return CreateServiceBusMessage(rejected, actor);
    }

    private static ServiceBusMessage CreateServiceBusMessage<TData>(
        TData data,
        Actor actor)
            where TData : class
    {
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_026.Name,
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
            idempotencyKey: Guid.NewGuid().ToString());
    }
}
