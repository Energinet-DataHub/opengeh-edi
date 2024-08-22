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

using System.Net;
using Energinet.DataHub.EDI.B2BApi.Common;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Dequeue;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.B2BApi.OutgoingMessages;

public class DequeueRequestListener
{
    private readonly AuthenticatedActor _authenticatedActor;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;

    public DequeueRequestListener(IOutgoingMessagesClient outgoingMessagesClient, AuthenticatedActor authenticatedActor)
    {
        _outgoingMessagesClient = outgoingMessagesClient;
        _authenticatedActor = authenticatedActor;
    }

    [Function("DequeueRequestListener")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "dequeue/{messageId}")]
        HttpRequestData request,
        FunctionContext executionContext,
        string messageId,
        CancellationToken hostCancellationToken)
    {
        var cancellationToken = request.GetCancellationToken(hostCancellationToken);
        var result = await _outgoingMessagesClient.DequeueAndCommitAsync(
                new DequeueRequestDto(
                    messageId,
                    _authenticatedActor.CurrentActorIdentity.MarketRole!,
                    _authenticatedActor.CurrentActorIdentity.ActorNumber),
                cancellationToken)
            .ConfigureAwait(false);

        return result.Success
            ? request.CreateResponse(HttpStatusCode.OK)
            : request.CreateResponse(HttpStatusCode.BadRequest);
    }
}
