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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

public class EnqueueWholesaleResultsForTotalAmountsActivity(
    ILogger<EnqueueWholesaleResultsForTotalAmountsActivity> logger,
    IServiceScopeFactory serviceScopeFactory,
    WholesaleResultEnumerator wholesaleResultEnumerator)
    : EnqueueWholesaleResultsBaseActivity(logger, serviceScopeFactory, wholesaleResultEnumerator)
{
    private readonly ILogger<EnqueueWholesaleResultsForTotalAmountsActivity> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly WholesaleResultEnumerator _wholesaleResultEnumerator = wholesaleResultEnumerator;

    /// <summary>
    /// Start an EnqueueWholesaleResultsForTotalAmountPerCharges activity.
    /// <remarks>The <paramref name="input"/> type and return type must be that same as the <see cref="Run"/> method</remarks>
    /// <remarks>Changing the <paramref name="input"/> or return type might break the Durable Function's deserialization</remarks>
    /// </summary>
    public static Task<int> StartActivityAsync(EnqueueMessagesForActorInput input, TaskOrchestrationContext context, TaskOptions? options)
    {
        return context.CallActivityAsync<int>(
            nameof(EnqueueWholesaleResultsForTotalAmountsActivity),
            input,
            options: options);
    }

    [Function(nameof(EnqueueWholesaleResultsForTotalAmountsActivity))]
    public Task<int> Run([ActivityTrigger] EnqueueMessagesForActorInput input)
    {
        var query = new WholesaleTotalAmountQuery(
            _logger,
            _wholesaleResultEnumerator.EdiDatabricksOptions,
            EventId.From(input.EventId),
            input.CalculationId,
            input.Actor);

        return EnqueueWholesaleResults(input, query);
    }

    protected override Task EnqueueAndCommitWholesaleResult<TOutgoingMessage>(IOutgoingMessagesClient outgoingMessagesClient, TOutgoingMessage outgoingMessageDto)
    {
        return outgoingMessageDto is WholesaleTotalAmountMessageDto totalAmountMessageDto
            ? outgoingMessagesClient.EnqueueAndCommitAsync(totalAmountMessageDto, CancellationToken.None)
            : throw new ArgumentException(
                $"The outgoing message dto is not of the expected type {typeof(WholesaleTotalAmountMessageDto).FullName}",
                nameof(outgoingMessageDto));
    }
}
