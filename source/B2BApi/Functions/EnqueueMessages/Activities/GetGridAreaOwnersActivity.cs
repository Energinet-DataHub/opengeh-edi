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

using System.Collections.Immutable;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

public class GetGridAreaOwnersActivity(IMasterDataClient masterDataClient)
{
    private readonly IMasterDataClient _masterDataClient = masterDataClient;

    /// <summary>
    /// Start a GetGridAreaOwners activity.
    /// <remarks>The <paramref name="input"/> type and return type must be that same as the <see cref="Run"/> method</remarks>
    /// <remarks>Changing the <paramref name="input"/> or return type might break the Durable Function's deserialization</remarks>
    /// </summary>
    public static Task<ImmutableDictionary<string, ActorNumber>> StartActivityAsync(
        TaskOrchestrationContext context,
        TaskOptions? options)
    {
        return context.CallActivityAsync<ImmutableDictionary<string, ActorNumber>>(
            nameof(GetGridAreaOwnersActivity),
            null,
            options: options);
    }

    [Function(nameof(GetGridAreaOwnersActivity))]
    public async Task<ImmutableDictionary<string, ActorNumber>> Run([ActivityTrigger] object? input)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, ActorNumber>();
        foreach (var gridAreaOwner in await _masterDataClient
                     .GetAllGridAreaOwnersAsync(CancellationToken.None)
                     .ConfigureAwait(false))
        {
            builder.Add(gridAreaOwner.GridAreaCode, gridAreaOwner.ActorNumber);
        }

        return builder.ToImmutable();
    }
}
