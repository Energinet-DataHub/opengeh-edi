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

using System;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.Application.ActorCertificate;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Actors;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Domain.ActorCertificates;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using MediatR;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;

public class ActorCertificateCredentialsAssignedMapper : IIntegrationEventMapper
{
    public string EventTypeToHandle => ActorCertificateCredentialsAssigned.EventName;

    public Task<ICommand<Unit>> MapToCommandAsync(IntegrationEvent integrationEvent)
    {
        if (integrationEvent == null)
            throw new ArgumentNullException(nameof(integrationEvent));

        var message = (ActorCertificateCredentialsAssigned)integrationEvent.Message;

        return Task.FromResult<ICommand<Unit>>(
            new ActorCertificateCredentialsAssignedCommand(
                ActorNumber.Create(message.ActorNumber),
                GetMarketRole(message.ActorRole),
                new CertificateThumbprint(message.CertificateThumbprint),
                message.ValidFrom.ToInstant(),
                message.SequenceNumber));
    }

    private static MarketRole GetMarketRole(EicFunction actorRole)
    {
        return actorRole switch
        {
            EicFunction.MeteringPointAdministrator => MarketRole.MeteringPointAdministrator,
            EicFunction.EnergySupplier => MarketRole.EnergySupplier,
            EicFunction.GridAccessProvider => MarketRole.GridOperator,
            EicFunction.MeteredDataAdministrator => MarketRole.MeteringDataAdministrator,
            EicFunction.MeteredDataResponsible => MarketRole.MeteredDataResponsible,
            EicFunction.BalanceResponsibleParty => MarketRole.BalanceResponsibleParty,
            // TODO: MarketRole.CalculationResponsibleRole and MarketRole.MasterDataResponsibleRole cannot be created, since they are duplicates
            _ => throw new ArgumentOutOfRangeException(nameof(actorRole), actorRole, "Unknown EicFunction market role value"),
        };

        // EicFunction.BillingAgent = ??
        // EicFunction.SystemOperator = ??
        // EicFunction.DanishEnergyAgency = ??
        // EicFunction.DatahubAdministrator = ??
        // EicFunction.IndependentAggregator = ?? DEA?
        // EicFunction.SerialEnergyTrader = ??
    }
}
