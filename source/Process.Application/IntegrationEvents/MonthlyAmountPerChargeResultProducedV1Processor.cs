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
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleCalculations;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.Process.Application.IntegrationEvents;

public class MonthlyAmountPerChargeResultProducedV1Processor : IIntegrationEventProcessor
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly IFeatureFlagManager _featureManager;
    private readonly ActorMessageQueueContext _actorMessageQueueContext;

    public MonthlyAmountPerChargeResultProducedV1Processor(
        IOutgoingMessagesClient outgoingMessagesClient,
        IFeatureFlagManager featureManager,
        ActorMessageQueueContext actorMessageQueueContext)
    {
        _outgoingMessagesClient = outgoingMessagesClient;
        _featureManager = featureManager;
        _actorMessageQueueContext = actorMessageQueueContext;
    }

    public string EventTypeToHandle => MonthlyAmountPerChargeResultProducedV1.EventName;

    public async Task ProcessAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var monthlyAmountPerChargeResultProducedV1 = (MonthlyAmountPerChargeResultProducedV1)integrationEvent.Message;

        var messageForEnergySupplier = WholesaleCalculationResultMessageFactory.CreateMessageForEnergySupplier(
            monthlyAmountPerChargeResultProducedV1,
            ProcessId.New());

        var messageForChargeOwner = WholesaleCalculationResultMessageFactory.CreateMessageForChargeOwner(
            monthlyAmountPerChargeResultProducedV1,
            ProcessId.New());

        if (await _featureManager.UseMonthlyAmountPerChargeResultProduced.ConfigureAwait(false))
        {
            await ResilientTransaction.New(_actorMessageQueueContext, async () =>
                {
                    await _outgoingMessagesClient.EnqueueAsync(messageForEnergySupplier).ConfigureAwait(false);
                    await _outgoingMessagesClient.EnqueueAsync(messageForChargeOwner).ConfigureAwait(false);
                })
                .SaveChangesAsync(new DbContext[] { _actorMessageQueueContext, })
                .ConfigureAwait(false);
        }
    }
}
