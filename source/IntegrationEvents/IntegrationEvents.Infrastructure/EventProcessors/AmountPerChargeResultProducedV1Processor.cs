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

using System;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.EventProcessors;

public class AmountPerChargeResultProducedV1Processor : IIntegrationEventProcessor
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly IFeatureFlagManager _featureManager;

    public AmountPerChargeResultProducedV1Processor(
        IOutgoingMessagesClient outgoingMessagesClient,
        IFeatureFlagManager featureManager)
    {
        _outgoingMessagesClient = outgoingMessagesClient;
        _featureManager = featureManager;
    }

    public string EventTypeToHandle => AmountPerChargeResultProducedV1.EventName;

    public async Task ProcessAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (!await _featureManager.UseAmountPerChargeResultProduced.ConfigureAwait(false))
        {
            return;
        }

        var amountPerChargeResultProducedV1 = (AmountPerChargeResultProducedV1)integrationEvent.Message;
        var message = WholesaleServicesMessageFactory.CreateMessage(amountPerChargeResultProducedV1);

        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
