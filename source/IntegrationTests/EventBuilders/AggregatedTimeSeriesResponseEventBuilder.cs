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

using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Components.Abstractions.BusinessValidation;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using NodaTime.Text;
using PMActorNumber = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorNumber;
using PMActorRole = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorRole;
using PMBusinessReason = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.BusinessReason;
using PMMeteringPointType = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.MeteringPointType;
using PMSettlementMethod = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.SettlementMethod;
using PMSettlementVersion = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.SettlementVersion;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

[SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Random not used for security")]
internal static class AggregatedTimeSeriesResponseEventBuilder
{
   public static ServiceBusMessage GenerateAcceptedFrom(
        RequestCalculatedEnergyTimeSeriesInputV1 requestCalculatedEnergyTimeSeriesInput,
        IReadOnlyCollection<string>? gridAreas = null,
        string? defaultChargeOwnerId = null,
        string? defaultEnergySupplierId = null)
    {
        var periodEnd = requestCalculatedEnergyTimeSeriesInput.PeriodEnd != null ?
            InstantPattern.General.Parse(requestCalculatedEnergyTimeSeriesInput.PeriodEnd).Value.ToDateTimeOffset()
            : throw new ArgumentNullException(nameof(requestCalculatedEnergyTimeSeriesInput.PeriodEnd), "PeriodEnd must be set");
        var energySupplierNumber = requestCalculatedEnergyTimeSeriesInput.EnergySupplierNumber != null
            ? PMActorNumber.Create(requestCalculatedEnergyTimeSeriesInput.EnergySupplierNumber)
            : defaultEnergySupplierId != null ? PMActorNumber.Create(defaultEnergySupplierId) : null;
        var balanceResponsibleNumber = requestCalculatedEnergyTimeSeriesInput.BalanceResponsibleNumber != null
            ? PMActorNumber.Create(requestCalculatedEnergyTimeSeriesInput.BalanceResponsibleNumber)
            : defaultChargeOwnerId != null ? PMActorNumber.Create(defaultChargeOwnerId) : null;
        var settlementVersion = requestCalculatedEnergyTimeSeriesInput.SettlementVersion != null
            ? PMSettlementVersion.FromName(requestCalculatedEnergyTimeSeriesInput.SettlementVersion)
            : null;
        var meteringPointType = requestCalculatedEnergyTimeSeriesInput.MeteringPointType != null
            ? PMMeteringPointType.FromName(requestCalculatedEnergyTimeSeriesInput.MeteringPointType)
            : null;
        var settlementMethod = requestCalculatedEnergyTimeSeriesInput.SettlementMethod != null
            ? PMSettlementMethod.FromName(requestCalculatedEnergyTimeSeriesInput.SettlementMethod)
            : null;
        var acceptedGridAreas = requestCalculatedEnergyTimeSeriesInput.GridAreas.Count != 0
            ? requestCalculatedEnergyTimeSeriesInput.GridAreas
            : gridAreas;

        var acceptRequest = new RequestCalculatedEnergyTimeSeriesAcceptedV1(
            OriginalActorMessageId: requestCalculatedEnergyTimeSeriesInput.ActorMessageId,
            OriginalTransactionId: requestCalculatedEnergyTimeSeriesInput.TransactionId,
            RequestedForActorNumber: PMActorNumber.Create(requestCalculatedEnergyTimeSeriesInput.RequestedForActorNumber),
            RequestedForActorRole: PMActorRole.FromName(requestCalculatedEnergyTimeSeriesInput.RequestedForActorRole),
            RequestedByActorNumber: PMActorNumber.Create(requestCalculatedEnergyTimeSeriesInput.RequestedByActorNumber),
            RequestedByActorRole: PMActorRole.FromName(requestCalculatedEnergyTimeSeriesInput.RequestedByActorRole),
            BusinessReason: PMBusinessReason.FromName(requestCalculatedEnergyTimeSeriesInput.BusinessReason),
            PeriodStart: InstantPattern.General.Parse(requestCalculatedEnergyTimeSeriesInput.PeriodStart).Value.ToDateTimeOffset(),
            PeriodEnd: periodEnd,
            GridAreas: acceptedGridAreas ?? throw new ArgumentNullException(nameof(acceptedGridAreas), "acceptedGridAreas must be set when request has no GridAreaCodes"),
            EnergySupplierNumber: energySupplierNumber,
            BalanceResponsibleNumber: balanceResponsibleNumber,
            MeteringPointType: meteringPointType,
            SettlementMethod: settlementMethod,
            SettlementVersion: settlementVersion);

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_026.Name,
            OrchestrationVersion = Brs_026.V1.Version,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = requestCalculatedEnergyTimeSeriesInput.RequestedByActorNumber,
                ActorRole = PMActorRole.FromName(requestCalculatedEnergyTimeSeriesInput.RequestedByActorRole).ToActorRoleV1(),
            },
            OrchestrationInstanceId = Guid.NewGuid().ToString(),
        };
        enqueueActorMessages.SetData(acceptRequest);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_026.Name),
            idempotencyKey: Guid.NewGuid().ToString());

        return serviceBusMessage;
    }

   public static ServiceBusMessage GenerateRejectedFrom(
        RequestCalculatedEnergyTimeSeriesInputV1 requestCalculatedEnergyTimeSeriesInput,
        string errorMessage,
        string errorCode)
    {
        var validationErrors = new List<ValidationErrorDto>()
        {
            new(
                errorMessage,
                errorCode),
        };
        var rejectRequest = new RequestCalculatedEnergyTimeSeriesRejectedV1(
            OriginalMessageId: requestCalculatedEnergyTimeSeriesInput.ActorMessageId,
            OriginalActorMessageId: requestCalculatedEnergyTimeSeriesInput.ActorMessageId,
            OriginalTransactionId: requestCalculatedEnergyTimeSeriesInput.TransactionId,
            RequestedForActorNumber: PMActorNumber.Create(requestCalculatedEnergyTimeSeriesInput.RequestedForActorNumber),
            RequestedForActorRole: PMActorRole.FromName(requestCalculatedEnergyTimeSeriesInput.RequestedForActorRole),
            RequestedByActorNumber: PMActorNumber.Create(requestCalculatedEnergyTimeSeriesInput.RequestedByActorNumber),
            RequestedByActorRole: PMActorRole.FromName(requestCalculatedEnergyTimeSeriesInput.RequestedByActorRole),
            BusinessReason: PMBusinessReason.FromName(requestCalculatedEnergyTimeSeriesInput.BusinessReason),
            validationErrors);

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_026.Name,
            OrchestrationVersion = Brs_026.V1.Version,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = requestCalculatedEnergyTimeSeriesInput.RequestedByActorNumber,
                ActorRole = PMActorRole.FromName(requestCalculatedEnergyTimeSeriesInput.RequestedByActorRole).ToActorRoleV1(),
            },
            OrchestrationInstanceId = Guid.NewGuid().ToString(),
        };
        enqueueActorMessages.SetData(rejectRequest);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_026.Name),
            idempotencyKey: Guid.NewGuid().ToString());

        return serviceBusMessage;
    }
}
