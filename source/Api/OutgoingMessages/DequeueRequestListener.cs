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

using System.Net;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.Api.OutgoingMessages;

public class DequeueRequestListener
{
    private readonly IOutGoingMessagesClient _outGoingMessagesClient;
    private readonly AuthenticatedActor _authenticatedActor;

    public DequeueRequestListener(IOutGoingMessagesClient outGoingMessagesClient, AuthenticatedActor authenticatedActor)
    {
        _outGoingMessagesClient = outGoingMessagesClient;
        _authenticatedActor = authenticatedActor;
    }

    [Function("DequeueRequestListener")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "dequeue/{messageId}"),]
        HttpRequestData request,
        FunctionContext executionContext,
        string messageId)
    {
        var result = await _outGoingMessagesClient.DequeueAsync(
            new DequeueRequestDto(
                messageId,
                _authenticatedActor.CurrentActorIdentity.MarketRole!,
                _authenticatedActor.CurrentActorIdentity.ActorNumber!)).ConfigureAwait(false);

        return result.Success
            ? request.CreateResponse(HttpStatusCode.OK)
            : request.CreateResponse(HttpStatusCode.BadRequest);
    }
}
