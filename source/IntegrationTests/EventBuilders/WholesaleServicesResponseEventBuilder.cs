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
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using NodaTime.Text;
using PMActorNumber = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorNumber;
using PMActorRole = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorRole;
using PMBusinessReason = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.BusinessReason;
using PMChargeType = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.ChargeType;
using PMResolution = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Resolution;
using PMSettlementVersion = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.SettlementVersion;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

public static class WholesaleServicesResponseEventBuilder
{
    /// <summary>
    /// Accepted Response from PM
    /// </summary>
    /// <param name="requestCalculatedWholesaleServicesInputV1"></param>
    /// <param name="gridAreas">All grid areas which PM finds for requester, when request is not limited by gridAreas</param>
    /// <param name="defaultChargeOwnerId"></param>
    /// <param name="defaultEnergySupplierId"></param>
    public static ServiceBusMessage GenerateAcceptedFrom(
        RequestCalculatedWholesaleServicesInputV1 requestCalculatedWholesaleServicesInputV1,
        IReadOnlyCollection<string>? gridAreas = null,
        string? defaultChargeOwnerId = null,
        string? defaultEnergySupplierId = null)
    {
        var chargeTypes = requestCalculatedWholesaleServicesInputV1.ChargeTypes?
            .Select(x => new RequestCalculatedWholesaleServicesAcceptedV1.AcceptedChargeType(PMChargeType.FromName(x.ChargeType!), x.ChargeCode))
            ?? new List<RequestCalculatedWholesaleServicesAcceptedV1.AcceptedChargeType>() { new(PMChargeType.Tariff, "25361478") };

        var periodEnd = requestCalculatedWholesaleServicesInputV1.PeriodEnd != null ?
            InstantPattern.General.Parse(requestCalculatedWholesaleServicesInputV1.PeriodEnd).Value.ToDateTimeOffset()
            : throw new ArgumentNullException(nameof(requestCalculatedWholesaleServicesInputV1.PeriodEnd), "PeriodEnd must be set");
        var energySupplierNumber = requestCalculatedWholesaleServicesInputV1.EnergySupplierNumber != null
            ? PMActorNumber.Create(requestCalculatedWholesaleServicesInputV1.EnergySupplierNumber)
            : defaultEnergySupplierId != null ? PMActorNumber.Create(defaultEnergySupplierId) : null;
        var chargeOwnerNumber = requestCalculatedWholesaleServicesInputV1.ChargeOwnerNumber != null
            ? PMActorNumber.Create(requestCalculatedWholesaleServicesInputV1.ChargeOwnerNumber)
            : defaultChargeOwnerId != null ? PMActorNumber.Create(defaultChargeOwnerId) : null;
        var settlementVersion = requestCalculatedWholesaleServicesInputV1.SettlementVersion != null
            ? PMSettlementVersion.FromName(requestCalculatedWholesaleServicesInputV1.SettlementVersion)
            : null;
        var resolution = requestCalculatedWholesaleServicesInputV1.Resolution != null
            ? PMResolution.FromName(requestCalculatedWholesaleServicesInputV1.Resolution)
            : null;
        var acceptedGridAreas = requestCalculatedWholesaleServicesInputV1.GridAreas.Count != 0
            ? requestCalculatedWholesaleServicesInputV1.GridAreas
            : gridAreas;
        var acceptRequest = new RequestCalculatedWholesaleServicesAcceptedV1(
            OriginalActorMessageId: requestCalculatedWholesaleServicesInputV1.ActorMessageId,
            OriginalTransactionId: requestCalculatedWholesaleServicesInputV1.TransactionId,
            RequestedForActorNumber: PMActorNumber.Create(requestCalculatedWholesaleServicesInputV1.RequestedForActorNumber),
            RequestedForActorRole: PMActorRole.FromName(requestCalculatedWholesaleServicesInputV1.RequestedForActorRole),
            RequestedByActorNumber: PMActorNumber.Create(requestCalculatedWholesaleServicesInputV1.RequestedByActorNumber),
            RequestedByActorRole: PMActorRole.FromName(requestCalculatedWholesaleServicesInputV1.RequestedByActorRole),
            BusinessReason: PMBusinessReason.FromName(requestCalculatedWholesaleServicesInputV1.BusinessReason),
            Resolution: resolution,
            PeriodStart: InstantPattern.General.Parse(requestCalculatedWholesaleServicesInputV1.PeriodStart).Value.ToDateTimeOffset(),
            PeriodEnd: periodEnd,
            GridAreas: acceptedGridAreas ?? throw new ArgumentNullException(nameof(acceptedGridAreas), "acceptedGridAreas must be set when request has no GridAreaCodes"),
            EnergySupplierNumber: energySupplierNumber,
            ChargeOwnerNumber: chargeOwnerNumber,
            SettlementVersion: settlementVersion,
            ChargeTypes: chargeTypes.ToList());

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_028.Name,
            OrchestrationVersion = Brs_028.V1.Version,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = requestCalculatedWholesaleServicesInputV1.RequestedByActorNumber,
                ActorRole = PMActorRole.FromName(requestCalculatedWholesaleServicesInputV1.RequestedByActorRole).ToActorRoleV1(),
            },
            OrchestrationInstanceId = Guid.NewGuid().ToString(),
        };
        enqueueActorMessages.SetData(acceptRequest);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_028.Name),
            idempotencyKey: Guid.NewGuid().ToString());

        return serviceBusMessage;
    }

    public static ServiceBusMessage GenerateRejectedFrom(
        RequestCalculatedWholesaleServicesInputV1 requestCalculatedWholesaleServicesInputV1,
        string errorMessage,
        string errorCode)
    {
        var validationErrors = new List<ValidationErrorDto>()
        {
            new(
                errorMessage,
                errorCode),
        };
        var rejectRequest = new RequestCalculatedWholesaleServicesRejectedV1(
            OriginalActorMessageId: requestCalculatedWholesaleServicesInputV1.ActorMessageId,
            OriginalTransactionId: requestCalculatedWholesaleServicesInputV1.TransactionId,
            RequestedForActorNumber: PMActorNumber.Create(requestCalculatedWholesaleServicesInputV1.RequestedForActorNumber),
            RequestedForActorRole: PMActorRole.FromName(requestCalculatedWholesaleServicesInputV1.RequestedForActorRole),
            RequestedByActorNumber: PMActorNumber.Create(requestCalculatedWholesaleServicesInputV1.RequestedByActorNumber),
            RequestedByActorRole: PMActorRole.FromName(requestCalculatedWholesaleServicesInputV1.RequestedByActorRole),
            BusinessReason: PMBusinessReason.FromName(requestCalculatedWholesaleServicesInputV1.BusinessReason),
            validationErrors);

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_028.Name,
            OrchestrationVersion = Brs_028.V1.Version,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = requestCalculatedWholesaleServicesInputV1.RequestedByActorNumber,
                ActorRole = PMActorRole.FromName(requestCalculatedWholesaleServicesInputV1.RequestedByActorRole).ToActorRoleV1(),
            },
            OrchestrationInstanceId = Guid.NewGuid().ToString(), // TODO, could be used to assert on when notifying the orchestration instance in pm
        };
        enqueueActorMessages.SetData(rejectRequest);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_028.Name),
            idempotencyKey: Guid.NewGuid().ToString());

        return serviceBusMessage;
    }
}
