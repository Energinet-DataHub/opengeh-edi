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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

public class EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity(
    IOutgoingMessagesClient outgoingMessagesClient)
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;

    [Function(nameof(EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity))]
    public async Task<int> Run(
        [ActivityTrigger] EnqueueMessagesInput inputDto)
    {
        try
        {
            var numberOfEnqueuedMessages = await _outgoingMessagesClient.EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersAsync(
                new EnqueueMessagesInputDto(
                    inputDto.CalculationId,
                    inputDto.EventId))
                .ConfigureAwait(false);

            return numberOfEnqueuedMessages;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}