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

using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationEvents.Application;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.EventProcessors;

public class ActorCertificateCredentialsRemovedEventProcessor : IIntegrationEventProcessor
{
    private readonly IMasterDataClient _masterDataClient;

    public ActorCertificateCredentialsRemovedEventProcessor(IMasterDataClient masterDataClient)
    {
        _masterDataClient = masterDataClient;
    }

    public string EventTypeToHandle => ActorCertificateCredentialsRemoved.EventName;

    public async Task ProcessAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var message = (ActorCertificateCredentialsRemoved)integrationEvent.Message;

        await _masterDataClient.DeleteActorCertificateAsync(
            new ActorCertificateCredentialsRemovedDto(
                ActorNumber.Create(message.ActorNumber),
                new CertificateThumbprintDto(message.CertificateThumbprint)),
            cancellationToken).ConfigureAwait(false);
    }
}
