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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.EnergyResults;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

// TODO: Decide where code for accessing DataLake should be placed; for now I'm just writing it as plain code within the activity.
// TODO: Decide if we need to reference NuGet package "Energinet.DataHub.Core.Databricks.SqlStatementExecution" directly here, or not.
internal class EnqueueMessagesActivity(
    IOutgoingMessagesClient outgoingMessagesClient,
    EnergyResultEnumerator energyResultEnumerator)
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;

    // TODO: Decide "view" (and hence enumerator) based on calculation type
    private readonly EnergyResultEnumerator _energyResultEnumerator = energyResultEnumerator;

    [Function(nameof(EnqueueMessagesActivity))]
    public async Task Run(
        [ActivityTrigger] EnqueueMessagesInput input)
    {
        var calculationId = Guid.Parse(input.CalculationId);
        await foreach (var nextMessage in _energyResultEnumerator.GetAsync(calculationId))
        {
            await _outgoingMessagesClient.EnqueueAndCommitAsync(nextMessage, CancellationToken.None);
        }
    }
}
