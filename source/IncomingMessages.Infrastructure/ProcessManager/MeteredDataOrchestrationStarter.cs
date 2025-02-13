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
using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ProcessManager;

[SuppressMessage(
    "StyleCop.CSharp.ReadabilityRules",
    "SA1118:Parameter should not span multiple lines",
    Justification = "Needed for inline ebix exception")]
public class MeteredDataOrchestrationStarter(IProcessManagerMessageClient processManagerMessageClient, AuthenticatedActor authenticatedActor)
{
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;
    private readonly AuthenticatedActor _authenticatedActor = authenticatedActor;

    public async Task StartForwardMeteredDataForMeteringPointOrchestrationAsync(
        InitializeMeteredDataForMeteringPointMessageProcessDto initializeProcessDto,
        CancellationToken cancellationToken)
    {
        var actorIdentityDto = GetAuthenticatedActorIdentity(initializeProcessDto.MessageId);

        var startProcessTasks = new List<Task>();
        foreach (var transaction in initializeProcessDto.Series)
        {
            var meteringPointType = transaction.MeteringPointType is not null
                ? MeteringPointType.TryGetNameFromCode(transaction.MeteringPointType, fallbackValue: transaction.MeteringPointType)
                : null;

            var productUnitType = transaction.ProductUnitType is not null
                ? MeasurementUnit.TryGetNameFromCode(transaction.ProductUnitType, fallbackValue: transaction.ProductUnitType)
                : null;

            var resolution = transaction.Resolution is not null
                ? Resolution.TryGetNameFromCode(transaction.Resolution, fallbackValue: transaction.Resolution)
                : null;

            var registeredAt = transaction.RegisteredAt is not null
                ? InstantPattern.General.Parse(transaction.RegisteredAt).Value.ToString()
                : null;

            var startCommand =
                new StartForwardMeteredDataCommandV1(
                    operatingIdentity: actorIdentityDto,
                    new MeteredDataForMeteringPointMessageInputV1(
                        MessageId: initializeProcessDto.MessageId,
                        AuthenticatedActorId: Guid.Empty, // TODO: Is this used?
                        ActorNumber: actorIdentityDto.ActorNumber,
                        ActorRole: actorIdentityDto.ActorRole, // TODO: Why was this code? This is now the name, which it should be.
                        TransactionId: transaction.TransactionId,
                        MeteringPointId: transaction.MeteringPointLocationId,
                        MeteringPointType: meteringPointType,
                        ProductNumber: transaction.ProductNumber,
                        MeasureUnit: productUnitType,
                        RegistrationDateTime: registeredAt
                                              ?? throw new ArgumentNullException(
                                                  nameof(transaction.RegisteredAt),
                                                  "RegistrationDateTime is only allowed to be null in Ebix."),
                        Resolution: resolution,
                        StartDateTime: transaction.StartDateTime,
                        EndDateTime: transaction.EndDateTime,
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
                    $"{transaction.RequestedByActor.ActorNumber.Value}-{transaction.TransactionId}");

            var startProcessTask = _processManagerMessageClient.StartNewOrchestrationInstanceAsync(startCommand, CancellationToken.None);
            startProcessTasks.Add(startProcessTask);
        }

        await Task.WhenAll(startProcessTasks).ConfigureAwait(false);
    }

    private ActorIdentityDto GetAuthenticatedActorIdentity(string messageId)
    {
        if (!_authenticatedActor.TryGetCurrentActorIdentity(out var actorIdentity))
            throw new InvalidOperationException($"Cannot get current actor when initializing process (MessageId={messageId})");

        var actor = actorIdentity?.ToActor()
                    ?? throw new InvalidOperationException($"Current actor identity was null when initializing process (MessageId={messageId})");

        return new ActorIdentityDto(
            ActorNumber: actor.ActorNumber.Value,
            ActorRole: actor.ActorRole.Name);
    }
}
