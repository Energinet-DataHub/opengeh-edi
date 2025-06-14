﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_024.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_025.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028.V1.Model;
using NodaTime;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ProcessManager;

public class RequestProcessOrchestrationStarter(
    IProcessManagerMessageClientFactory processManagerMessageClientFactory,
    AuthenticatedActor authenticatedActor,
    IClock clock) : IRequestProcessOrchestrationStarter
{
    private readonly IProcessManagerMessageClientFactory _processManagerMessageClientFactory = processManagerMessageClientFactory;
    private readonly AuthenticatedActor _authenticatedActor = authenticatedActor;
    private readonly IClock _clock = clock;

    public async Task StartRequestWholesaleServicesOrchestrationAsync(
        InitializeWholesaleServicesProcessDto initializeProcessDto,
        CancellationToken cancellationToken)
    {
        var actorIdentity = GetAuthenticatedActorIdentityDto(initializeProcessDto.MessageId);
        var processManagerMessageClient = _processManagerMessageClientFactory.CreateMessageClient(initializeProcessDto.MessageId);

        var startProcessTasks = new List<Task>();
        foreach (var transaction in initializeProcessDto.Series)
        {
            var resolution = transaction.Resolution != null
                ? Resolution.TryGetNameFromCode(transaction.Resolution, fallbackValue: transaction.Resolution)
                : null;

            var settlementVersion = transaction.SettlementVersion is not null
                ? SettlementVersion.TryGetNameFromCode(transaction.SettlementVersion, fallbackValue: transaction.SettlementVersion)
                : null;

            var chargeTypes = transaction.ChargeTypes.Select(
                ct =>
                {
                    var chargeType = ct.Type != null
                        ? ChargeType.TryGetNameFromCode(ct.Type, fallbackValue: ct.Type)
                        : null;

                    return new RequestCalculatedWholesaleServicesInputV1.ChargeTypeInput(
                        ChargeType: chargeType,
                        ChargeCode: ct.Id);
                })
                .ToList();

            var startCommand = new RequestCalculatedWholesaleServicesCommandV1(
                operatingIdentity: actorIdentity,
                inputParameter: new RequestCalculatedWholesaleServicesInputV1(
                    ActorMessageId: initializeProcessDto.MessageId,
                    TransactionId: transaction.Id,
                    RequestedForActorNumber: transaction.OriginalActor.ActorNumber.Value,
                    RequestedForActorRole: transaction.OriginalActor.ActorRole.Name,
                    RequestedByActorNumber: transaction.RequestedByActor.ActorNumber.Value,
                    RequestedByActorRole: transaction.RequestedByActor.ActorRole.Name,
                    BusinessReason: BusinessReason.TryGetNameFromCode(initializeProcessDto.BusinessReason, fallbackValue: initializeProcessDto.BusinessReason),
                    Resolution: resolution,
                    PeriodStart: transaction.StartDateTime,
                    PeriodEnd: transaction.EndDateTime,
                    EnergySupplierNumber: transaction.EnergySupplierId,
                    ChargeOwnerNumber: transaction.ChargeOwner,
                    GridAreas: transaction.GridAreas,
                    SettlementVersion: settlementVersion,
                    ChargeTypes: chargeTypes),
                idempotencyKey: CreateIdempotencyKey(transaction.Id, transaction.RequestedByActor));

            // TODO: Handle resiliency. Could use something like Polly to retry if failing?
            var startProcessTask = processManagerMessageClient.StartNewOrchestrationInstanceAsync(startCommand, cancellationToken);
            startProcessTasks.Add(startProcessTask);
        }

        await Task.WhenAll(startProcessTasks).ConfigureAwait(false);
    }

    public async Task StartRequestAggregatedMeasureDataOrchestrationAsync(
        InitializeAggregatedMeasureDataProcessDto initializeProcessDto,
        CancellationToken cancellationToken)
    {
        var actorIdentity = GetAuthenticatedActorIdentityDto(initializeProcessDto.MessageId);
        var processManagerMessageClient = _processManagerMessageClientFactory.CreateMessageClient(initializeProcessDto.MessageId);

        var startProcessTasks = new List<Task>();
        foreach (var transaction in initializeProcessDto.Series)
        {
            var meteringPointType = transaction.MeteringPointType is not null
                ? MeteringPointType.TryGetNameFromCode(transaction.MeteringPointType, fallbackValue: transaction.MeteringPointType)
                : null;

            var settlementMethod = transaction.SettlementMethod is not null
                ? SettlementMethod.TryGetNameFromCode(transaction.SettlementMethod, fallbackValue: transaction.SettlementMethod)
                : null;

            var settlementVersion = transaction.SettlementVersion is not null
                ? SettlementVersion.TryGetNameFromCode(transaction.SettlementVersion, fallbackValue: transaction.SettlementVersion)
                : null;

            var startCommand = new RequestCalculatedEnergyTimeSeriesCommandV1(
                operatingIdentity: actorIdentity,
                inputParameter: new RequestCalculatedEnergyTimeSeriesInputV1(
                    ActorMessageId: initializeProcessDto.MessageId,
                    TransactionId: transaction.Id.Value,
                    RequestedForActorNumber: transaction.OriginalActor.ActorNumber.Value,
                    RequestedForActorRole: transaction.OriginalActor.ActorRole.Name,
                    RequestedByActorNumber: transaction.RequestedByActor.ActorNumber.Value,
                    RequestedByActorRole: transaction.RequestedByActor.ActorRole.Name,
                    BusinessReason: BusinessReason.TryGetNameFromCode(initializeProcessDto.BusinessReason, fallbackValue: initializeProcessDto.BusinessReason),
                    PeriodStart: transaction.StartDateTime,
                    PeriodEnd: transaction.EndDateTime,
                    EnergySupplierNumber: transaction.EnergySupplierNumber,
                    BalanceResponsibleNumber: transaction.BalanceResponsibleNumber,
                    GridAreas: transaction.GridAreas,
                    MeteringPointType: meteringPointType,
                    SettlementMethod: settlementMethod,
                    SettlementVersion: settlementVersion),
                idempotencyKey: CreateIdempotencyKey(transaction.Id.Value, transaction.RequestedByActor));

            // TODO: Handle resiliency. Could use something like Polly to retry if failing?
            var startProcessTask = processManagerMessageClient.StartNewOrchestrationInstanceAsync(startCommand, cancellationToken);
            startProcessTasks.Add(startProcessTask);
        }

        await Task.WhenAll(startProcessTasks).ConfigureAwait(false);
    }

    public async Task StartRequestYearlyMeasurementsOrchestrationAsync(
        InitializeRequestMeasurementsProcessDto initializeProcessDto,
        CancellationToken cancellationToken)
    {
        var actorIdentity = GetAuthenticatedActorIdentityDto(initializeProcessDto.MessageId);
        var processManagerMessageClient = _processManagerMessageClientFactory.CreateMessageClient(initializeProcessDto.MessageId);

        var startProcessTasks = new List<Task>();
        foreach (var transaction in initializeProcessDto.Series)
        {
            var startCommand = new RequestYearlyMeasurementsCommandV1(
                OperatingIdentity: actorIdentity,
                InputParameter: new RequestYearlyMeasurementsInputV1(
                    ActorMessageId: initializeProcessDto.MessageId,
                    TransactionId: transaction.Id.Value,
                    ActorNumber: transaction.OriginalActor.ActorNumber.Value,
                    ActorRole: transaction.OriginalActor.ActorRole.Name,
                    ReceivedAt: _clock.GetCurrentInstant().ToString(),
                    MeteringPointId: transaction.MeteringPointId.Value),
                IdempotencyKey: CreateIdempotencyKey(transaction.Id.Value, transaction.OriginalActor));

            var startProcessTask = processManagerMessageClient.StartNewOrchestrationInstanceAsync(startCommand, cancellationToken);
            startProcessTasks.Add(startProcessTask);
        }

        await Task.WhenAll(startProcessTasks).ConfigureAwait(false);
    }

    public async Task StartRequestMeasurementsOrchestrationAsync(
        InitializeRequestMeasurementsProcessDto initializeProcessDto,
        CancellationToken cancellationToken)
    {
        var actorIdentity = GetAuthenticatedActorIdentityDto(initializeProcessDto.MessageId);
        var processManagerMessageClient = _processManagerMessageClientFactory.CreateMessageClient(initializeProcessDto.MessageId);

        var startProcessTasks = new List<Task>();
        foreach (var transaction in initializeProcessDto.Series)
        {
            var startCommand = new RequestMeasurementsCommandV1(
                OperatingIdentity: actorIdentity,
                InputParameter: new RequestMeasurementsInputV1(
                    ActorMessageId: initializeProcessDto.MessageId,
                    TransactionId: transaction.Id.Value,
                    ActorNumber: transaction.OriginalActor.ActorNumber.Value,
                    ActorRole: transaction.OriginalActor.ActorRole.Name,
                    MeteringPointId: transaction.MeteringPointId.Value,
                    StartDateTime: transaction.StartDateTime,
                    EndDateTime: transaction.EndDateTime),
                IdempotencyKey: CreateIdempotencyKey(transaction.Id.Value, transaction.OriginalActor));

            var startProcessTask = processManagerMessageClient.StartNewOrchestrationInstanceAsync(startCommand, cancellationToken);
            startProcessTasks.Add(startProcessTask);
        }

        await Task.WhenAll(startProcessTasks).ConfigureAwait(false);
    }

    private ActorIdentityDto GetAuthenticatedActorIdentityDto(string messageId)
    {
        if (!_authenticatedActor.TryGetCurrentActorIdentity(out var actorIdentity))
            throw new InvalidOperationException($"Cannot get current actor when initializing process (MessageId={messageId})");

        var actor = actorIdentity?.ToActor()
               ?? throw new InvalidOperationException($"Current actor identity was null when initializing process (MessageId={messageId})");

        return new ActorIdentityDto(
            ActorNumber: actor.ActorNumber.ToProcessManagerActorNumber(),
            ActorRole: actor.ActorRole.ToProcessManagerActorRole());
    }

    private string CreateIdempotencyKey(string transactionId, RequestedByActor actor) => $"{transactionId}_{actor.ActorNumber.Value}_{actor.ActorRole.Code}";

    private string CreateIdempotencyKey(string transactionId, OriginalActor actor) => $"{transactionId}_{actor.ActorNumber.Value}_{actor.ActorRole.Code}";
}
