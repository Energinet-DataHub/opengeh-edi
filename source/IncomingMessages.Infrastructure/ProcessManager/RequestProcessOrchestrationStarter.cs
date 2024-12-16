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

using System.Collections.ObjectModel;
using System.Globalization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_028.V1.Model;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ProcessManager;

public class RequestProcessOrchestrationStarter(
    IProcessManagerMessageClient processManagerMessageClient,
    AuthenticatedActor authenticatedActor) : IRequestProcessOrchestrationStarter
{
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;
    private readonly AuthenticatedActor _authenticatedActor = authenticatedActor;

    public async Task StartRequestWholesaleServicesOrchestrationAsync(
        InitializeWholesaleServicesProcessDto initializeProcessDto,
        CancellationToken cancellationToken)
    {
        var actorId = GetAuthenticatedActorId(initializeProcessDto.MessageId);
        var actorIdentity = new ActorIdentityDto(actorId);

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

                    return new RequestCalculatedWholesaleServicesInputV1.ChargeTypeInputV1(
                        ChargeType: chargeType,
                        ChargeCode: ct.Id);
                })
                .ToList();

            var startCommand = new RequestCalculatedWholesaleServicesCommandV1(
                operatingIdentity: actorIdentity,
                inputParameter: new RequestCalculatedWholesaleServicesInputV1(
                    RequestedForActorNumber: transaction.OriginalActor.ActorNumber.Value,
                    RequestedForActorRole: transaction.OriginalActor.ActorRole.Name,
                    BusinessReason: BusinessReason.TryGetNameFromCode(initializeProcessDto.BusinessReason, fallbackValue: initializeProcessDto.BusinessReason),
                    Resolution: resolution,
                    PeriodStart: transaction.StartDateTime,
                    PeriodEnd: transaction.EndDateTime,
                    EnergySupplierNumber: transaction.EnergySupplierId,
                    ChargeOwnerNumber: transaction.ChargeOwner,
                    GridAreas: transaction.GridAreas,
                    SettlementVersion: settlementVersion,
                    ChargeTypes: chargeTypes),
                messageId: transaction.Id);

            // TODO: Handle resiliency. Could use something like Polly to retry if failing?
            var startProcessTask = _processManagerMessageClient.StartNewOrchestrationInstanceAsync(startCommand, cancellationToken);
            startProcessTasks.Add(startProcessTask);
        }

        await Task.WhenAll(startProcessTasks).ConfigureAwait(false);
    }

    public async Task StartRequestAggregatedMeasureDataOrchestrationAsync(
        InitializeAggregatedMeasureDataProcessDto initializeProcessDto,
        CancellationToken cancellationToken)
    {
        var actorId = GetAuthenticatedActorId(initializeProcessDto.MessageId);
        var actorIdentity = new ActorIdentityDto(actorId);

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

            var startCommand = new StartRequestCalculatedEnergyTimeSeriesCommandV1(
                operatingIdentity: actorIdentity,
                inputParameter: new RequestCalculatedEnergyTimeSeriesInputV1(
                    RequestedForActorNumber: transaction.OriginalActor.ActorNumber.Value,
                    RequestedForActorRole: transaction.OriginalActor.ActorRole.Name,
                    BusinessReason: BusinessReason.TryGetNameFromCode(initializeProcessDto.BusinessReason, fallbackValue: initializeProcessDto.BusinessReason),
                    PeriodStart: transaction.StartDateTime,
                    PeriodEnd: transaction.EndDateTime,
                    EnergySupplierNumber: transaction.EnergySupplierNumber,
                    BalanceResponsibleNumber: transaction.BalanceResponsibleNumber,
                    GridAreas: transaction.GridAreas,
                    MeteringPointType: meteringPointType,
                    SettlementMethod: settlementMethod,
                    SettlementVersion: settlementVersion),
                messageId: transaction.Id.Value);

            // TODO: Handle resiliency. Could use something like Polly to retry if failing?
            var startProcessTask = _processManagerMessageClient.StartNewOrchestrationInstanceAsync(startCommand, cancellationToken);
            startProcessTasks.Add(startProcessTask);
        }

        await Task.WhenAll(startProcessTasks).ConfigureAwait(false);
    }

    public async Task StartMeteredDataForMeasurementPointOrchestrationAsync(
        InitializeMeteredDataForMeasurementPointMessageProcessDto initializeProcessDto,
        CancellationToken cancellationToken)
    {
        var actorId = GetAuthenticatedActorId(initializeProcessDto.MessageId);
        var actorIdentity = new ActorIdentityDto(actorId);

        var datePattern = "yyyy-MM-ddTHH:mm'Z'";

        foreach (var transaction in initializeProcessDto.Series)
        {
            await _processManagerMessageClient.StartNewOrchestrationInstanceAsync(
                new StartForwardMeteredDataCommandV1(
                    operatingIdentity: actorIdentity,
                    new MeteredDataForMeasurementPointMessageInputV1(
                        AuthenticatedActorId: actorId,
                        TransactionId: transaction.TransactionId,
                        MeteringPointId: transaction.MeteringPointLocationId,
                        MeteringPointType: transaction.MeteringPointType,
                        ProductNumber: transaction.ProductNumber,
                        MeasureUnit: MeasurementUnit.FromCode(transaction.ProductUnitType!).Code,
                        RegistrationDateTime: InstantPattern.General.Parse(initializeProcessDto.CreatedAt).Value.ToString(),
                        Resolution: Resolution.FromCode(transaction.Resolution!).Code,
                        StartDateTime: InstantPattern.Create(datePattern, CultureInfo.InvariantCulture).Parse(transaction.StartDateTime).Value.ToString(),
                        EndDateTime: transaction.EndDateTime != null ? InstantPattern.Create(datePattern, CultureInfo.InvariantCulture).Parse(transaction.EndDateTime).Value.ToString() : throw new ArgumentNullException(),
                        GridAccessProviderNumber: transaction.RequestedByActor.ActorNumber.Value,
                        DelegatedGridAreaCodes: transaction.DelegatedGridAreaCodes,
                        EnergyObservations:
                            new ReadOnlyCollection<EnergyObservation>(
                                transaction.EnergyObservations
                                    .Select(energyObservation =>
                                        new EnergyObservation(
                                            Position: energyObservation.Position,
                                            EnergyQuantity: energyObservation.EnergyQuantity,
                                            QuantityQuality: energyObservation.QuantityQuality))
                                    .ToList())),
                    initializeProcessDto.MessageId),
                CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    private Guid GetAuthenticatedActorId(string messageId)
    {
        if (!_authenticatedActor.TryGetCurrentActorIdentity(out var actorIdentity))
            throw new InvalidOperationException($"Cannot get current actor when initializing process (MessageId={messageId})");

        return actorIdentity?.ActorId
               ?? throw new InvalidOperationException($"Current actor id was null when initializing process (MessageId={messageId})");
    }
}
