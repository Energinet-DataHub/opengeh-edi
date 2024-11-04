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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages.Request;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.WholesaleServices;
using Energinet.DataHub.Wholesale.Edi;
using Energinet.DataHub.Wholesale.Edi.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace Energinet.DataHub.EDI.B2BApi.Functions.RequestWholesaleServices.Activities;

public class EnqueueMessagesForRequestWholesaleServicesActivity(
    RequestWholesaleServicesQueryHandler requestWholesaleServicesQueryHandler,
    IOutgoingMessagesClient outgoingMessagesClient,
    IUnitOfWork unitOfWork)
{
    private readonly RequestWholesaleServicesQueryHandler _requestWholesaleServicesQueryHandler = requestWholesaleServicesQueryHandler;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// Start an ValidateWholesaleServicesRequestActivity activity.
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
        var calculationResults = await _requestWholesaleServicesQueryHandler.GetAsync(
            request,
            transaction.BusinessTransactionId.Value,
            cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<Guid> acceptedMessagesIds = [];
        List<Guid> rejectedMessageIds = [];
        foreach (var calculationResult in calculationResults)
        {
            if (calculationResult.Result == RequestWholesaleServicesQueryResultEnum.Success)
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
            }
            else if (calculationResult.Result == RequestWholesaleServicesQueryResultEnum.NoDataForGridArea)
            {
                // var rejectedWholesaleServicesMessage = RejectedWholesaleServiceMessageDtoFactory.Create();
                // var messageId = await _outgoingMessagesClient.EnqueueAsync(rejectedWholesaleServicesMessage, cancellationToken)
                //     .ConfigureAwait(false);
                rejectedMessageIds.Add(Guid.Empty);
            }
        }

        // TODO: Should this save per transaction (as it works today) or is it better to save once when all messages are enqueued?
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
