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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.WholesaleServices;
using Energinet.DataHub.Wholesale.Edi;
using Energinet.DataHub.Wholesale.Edi.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace Energinet.DataHub.EDI.B2BApi.Functions.RequestWholesaleServices.Activities;

/// <summary>
/// Enqueue messages for a Wholesale services request transaction.
/// Queries the requested data from databricks and enqueues a message for each result.
/// </summary>
public class EnqueueMessagesForRequestWholesaleServicesActivity(
    RequestWholesaleServicesQueryHandler requestWholesaleServicesQueryHandler,
    IOutgoingMessagesClient outgoingMessagesClient,
    IUnitOfWork unitOfWork)
{
    private readonly RequestWholesaleServicesQueryHandler _requestWholesaleServicesQueryHandler = requestWholesaleServicesQueryHandler;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// Start a <see cref="EnqueueMessagesForRequestWholesaleServicesActivity"/> activity.
    /// <remarks>The <paramref name="input"/> type and return type must be that same as the <see cref="Run"/> method</remarks>
    /// <remarks>Changing the <paramref name="input"/> or return type might break the Durable Function's deserialization</remarks>
    /// </summary>
    public static Task<EnqueueMessagesResult> StartActivityAsync(
        RequestWholesaleServicesTransaction input,
        TaskOrchestrationContext context,
        TaskOptions? options)
    {
        return context.CallActivityAsync<EnqueueMessagesResult>(
            nameof(EnqueueMessagesForRequestWholesaleServicesActivity),
            input,
            options: options);
    }

    [Function(nameof(EnqueueMessagesForRequestWholesaleServicesActivity))]
    public async Task<EnqueueMessagesResult> Run([ActivityTrigger] RequestWholesaleServicesTransaction transaction, CancellationToken cancellationToken)
    {
        var request = WholesaleServicesRequestFactory.CreateWholesaleServicesRequest(transaction);
        var calculationResultsAsyncEnumerable = _requestWholesaleServicesQueryHandler.GetAsync(
            request,
            transaction.BusinessTransactionId.Value,
            cancellationToken);

        List<Guid> acceptedMessagesIds = [];
        List<Guid> rejectedMessageIds = [];
        await foreach (var calculationResult in calculationResultsAsyncEnumerable)
        {
            switch (calculationResult.Result)
            {
                case RequestWholesaleServicesQueryResultEnum.Success:
                    {
                        ArgumentNullException.ThrowIfNull(calculationResult.WholesaleServices);
                        var acceptedWholesaleServicesMessage =
                            AcceptedWholesaleServiceMessageDtoFactory.Create(
                                EventId.From("deprecated-event-id"),
                                transaction,
                                calculationResult.WholesaleServices);
                        var messageId = await _outgoingMessagesClient.EnqueueAsync(acceptedWholesaleServicesMessage, cancellationToken)
                            .ConfigureAwait(false);

                        acceptedMessagesIds.Add(messageId);
                        break;
                    }
                case RequestWholesaleServicesQueryResultEnum.NoDataForGridArea:
                    // TODO: Enqueue rejected message
                case RequestWholesaleServicesQueryResultEnum.NoDataAvailable:
                    // TODO: Enqueue rejected message
                    // var rejectedWholesaleServicesMessage = RejectedWholesaleServiceMessageDtoFactory.Create();
                    // var messageId = await _outgoingMessagesClient.EnqueueAsync(rejectedWholesaleServicesMessage, cancellationToken)
                    //     .ConfigureAwait(false);
                    rejectedMessageIds.Add(Guid.Empty);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(calculationResult.Result), calculationResult.Result, "Unknown calculation result state when enqueueing messages");
            }
        }

        await _unitOfWork.CommitTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        return new EnqueueMessagesResult(
            acceptedMessagesIds,
            acceptedMessagesIds.Count,
            rejectedMessageIds,
            rejectedMessageIds.Count);
    }

    public record EnqueueMessagesResult(
        List<Guid> AcceptedMessageIds,
        int AcceptedMessagesCount,
        List<Guid> RejectedMessageIds,
        int RejectedMessagesCount);
}
