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
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleCalculations;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;

namespace Energinet.DataHub.EDI.Process.Application.IntegrationEvents;

public class MonthlyAmountPerChargeResultProducedV1Processor : IIntegrationEventProcessor
{
    private readonly WholesaleCalculationResultMessageFactory _wholesaleFactory;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly IFeatureFlagManager _featureManager;

    public MonthlyAmountPerChargeResultProducedV1Processor(
        WholesaleCalculationResultMessageFactory wholesaleFactory,
        IOutgoingMessagesClient outgoingMessagesClient,
        IFeatureFlagManager featureManager)
    {
        _wholesaleFactory = wholesaleFactory;
        _outgoingMessagesClient = outgoingMessagesClient;
        _featureManager = featureManager;
    }

    public string EventTypeToHandle => MonthlyAmountPerChargeResultProducedV1.EventName;

    public async Task ProcessAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var monthlyAmountPerChargeResultProducedV1 = (MonthlyAmountPerChargeResultProducedV1)integrationEvent.Message;

        var message = _wholesaleFactory.CreateMessage(monthlyAmountPerChargeResultProducedV1, ProcessId.New());

        if (await _featureManager.UseMonthlyAmountPerChargeResultProduced.ConfigureAwait(false))
        {
            await _outgoingMessagesClient.EnqueueAndCommitAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }
}
