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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.CalculationResults.Interfaces.Model.EnergyResults;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.EnergyResults;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

internal class EnqueueMessagesActivity(
    IOutgoingMessagesClient outgoingMessagesClient,
    EnergyResultEnumerator energyResultEnumerator)
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly EnergyResultEnumerator _energyResultEnumerator = energyResultEnumerator;

    [Function(nameof(EnqueueMessagesActivity))]
    public async Task Run(
        [ActivityTrigger] EnqueueMessagesInput input)
    {
        // TODO: Decide "view" / "query" based on calculation type
        var calculationId = Guid.Parse(input.CalculationId);
        await foreach (var nextResult in _energyResultEnumerator.GetAsync(calculationId))
        {
            var nextMessage = CreateMessage(nextResult);
            await _outgoingMessagesClient.EnqueueAndCommitAsync(nextMessage, CancellationToken.None);
        }
    }

    // TODO: Map from energy result to outgoing message
    private EnergyResultMessageDto CreateMessage(EnergyResultPerGridArea nextResult)
    {
        throw new NotImplementedException();
    }
}
