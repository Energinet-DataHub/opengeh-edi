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

using Energinet.DataHub.EDI.SubsystemTests.Factories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using Google.Protobuf;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers;

internal sealed class MarketParticipantDriver
{
    private readonly IntegrationEventPublisher _integrationEventPublisher;

    internal MarketParticipantDriver(IntegrationEventPublisher integrationEventPublisher)
    {
        _integrationEventPublisher = integrationEventPublisher;
    }

    internal async Task PublishActorActivatedAsync(string actorNumber, string b2cId)
    {
        await _integrationEventPublisher.PublishAsync(
            ActorActivated.EventName,
            ActorFactory.CreateActorActivated(actorNumber, b2cId).ToByteArray(),
            waitForHandled: true).ConfigureAwait(false);
    }

    internal async Task PublishActorCertificateCredentialsRemovedAsync(string actorNumber, string actorRole, string thumbprint)
    {
        await _integrationEventPublisher.PublishAsync(
                ActorCertificateCredentialsRemoved.EventName,
                ActorCertificateFactory.CreateActorCertificateCredentialsRemoved(actorNumber, actorRole, thumbprint).ToByteArray(),
                waitForHandled: true)
            .ConfigureAwait(false);
    }

    internal async Task PublishActorCertificateCredentialsAssignedAsync(string actorNumber, string actorRole, string thumbprint)
    {
        await _integrationEventPublisher.PublishAsync(
                ActorCertificateCredentialsAssigned.EventName,
                ActorCertificateFactory.CreateActorCertificateAssigned(actorNumber, actorRole, thumbprint).ToByteArray(),
                waitForHandled: true)
            .ConfigureAwait(false);
    }
}
