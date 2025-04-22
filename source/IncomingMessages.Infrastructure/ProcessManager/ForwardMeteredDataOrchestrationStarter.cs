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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ProcessManager;

[SuppressMessage(
    "StyleCop.CSharp.ReadabilityRules",
    "SA1118:Parameter should not span multiple lines",
    Justification = "Needed for inline ebix exception")]
public class ForwardMeteredDataOrchestrationStarter(IProcessManagerMessageClient processManagerMessageClient, AuthenticatedActor authenticatedActor)
{
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;
    private readonly AuthenticatedActor _authenticatedActor = authenticatedActor;

    public async Task StartForwardMeteredDataOrchestrationAsync(
        MeteredDataForMeteringPointMessageBase meteredDataForMeteringPointMessageBase,
        CancellationToken cancellationToken)
    {
        var actorIdentityDto = GetAuthenticatedActorIdentityDto(meteredDataForMeteringPointMessageBase.MessageId);

        var startProcessTasks = new List<Task>();
        foreach (var transaction in meteredDataForMeteringPointMessageBase.Series
                     .Cast<MeteredDataForMeteringPointSeries>())
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

            var businessReason = BusinessReason
                .TryGetNameFromCode(
                    meteredDataForMeteringPointMessageBase.BusinessReason,
                    fallbackValue: meteredDataForMeteringPointMessageBase.BusinessReason);

            var senderActorRole = ActorRole.TryGetNameFromCode(
                meteredDataForMeteringPointMessageBase.SenderRoleCode,
                meteredDataForMeteringPointMessageBase.SenderRoleCode);

            var startCommand =
                new ForwardMeteredDataCommandV1(
                    operatingIdentity: actorIdentityDto,
                    new ForwardMeteredDataInputV1(
                        ActorMessageId: meteredDataForMeteringPointMessageBase.MessageId,
                        TransactionId: transaction.TransactionId,
                        ActorNumber: meteredDataForMeteringPointMessageBase.SenderNumber,
                        ActorRole: senderActorRole,
                        BusinessReason: businessReason,
                        MeteringPointId: transaction.MeteringPointLocationId,
                        MeteringPointType: meteringPointType,
                        ProductNumber: transaction.ProductNumber,
                        MeasureUnit: productUnitType,
                        RegistrationDateTime: transaction.RegisteredAt,
                        Resolution: resolution,
                        StartDateTime: transaction.StartDateTime,
                        EndDateTime: transaction.EndDateTime,
                        GridAccessProviderNumber: meteredDataForMeteringPointMessageBase.SenderNumber,
                        MeteredDataList:
                                transaction.EnergyObservations
                                    .Select(MapToMeteredData)
                                    .ToList()),
                    $"{transaction.SenderNumber}-{transaction.TransactionId}");

            var startProcessTask =
                _processManagerMessageClient.StartNewOrchestrationInstanceAsync(startCommand, cancellationToken);
            startProcessTasks.Add(startProcessTask);
        }

        await Task.WhenAll(startProcessTasks).ConfigureAwait(false);
    }

    private static ForwardMeteredDataInputV1.MeteredData MapToMeteredData(EnergyObservation energyObservation)
    {
        var quantityQuality = energyObservation.QuantityQuality is not null
            ? Quality.TryGetNameFromCode(energyObservation.QuantityQuality, fallbackValue: energyObservation.QuantityQuality)
            : null;

        // We could not translate the CIM code, trying ebix
        if (quantityQuality == energyObservation.QuantityQuality)
        {
            quantityQuality = Quality.TryGetNameFromEbixCode(energyObservation.QuantityQuality, fallbackValue: energyObservation.QuantityQuality);
        }

        return new ForwardMeteredDataInputV1.MeteredData(
            Position: energyObservation.Position,
            EnergyQuantity: energyObservation.EnergyQuantity,
            QuantityQuality: quantityQuality);
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
}
