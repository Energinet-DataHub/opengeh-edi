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
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.EventProcessors;

public sealed class CalculationCompletedV1Processor : IIntegrationEventProcessor
{
    private readonly ILogger<CalculationCompletedV1Processor> _logger;
    private readonly IFeatureFlagManager _featureManager;

    public CalculationCompletedV1Processor(
        ILogger<CalculationCompletedV1Processor> logger,
        IFeatureFlagManager featureManager)
    {
        _logger = logger;
        _featureManager = featureManager;
    }

    public string EventTypeToHandle => CalculationCompletedV1.EventName;

    public async Task ProcessAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (!await _featureManager.UseCalculationCompletedEventAsync().ConfigureAwait(false))
        {
            return;
        }

        var message = (CalculationCompletedV1)integrationEvent.Message;

        // TODO: Handle event
        return;
    }
}