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

using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.ActorMessageQueue.Application.OutgoingMessages;
using Energinet.DataHub.EDI.ActorMessageQueue.Contracts;
using Energinet.DataHub.EDI.Application.Configuration.Authentication;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.Api.OutgoingMessages;

public class DequeueRequestListener
{
    private readonly IMediator _mediator;
    private readonly IMarketActorAuthenticator _authenticator;

    public DequeueRequestListener(IMediator mediator, IMarketActorAuthenticator authenticator)
    {
        _mediator = mediator;
        _authenticator = authenticator;
    }

    [Function("DequeueRequestListener")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "dequeue/{messageId}"),]
        HttpRequestData request,
        FunctionContext executionContext,
        string messageId)
    {
        var result = await _mediator.Send(new DequeueCommand(messageId, _authenticator.CurrentIdentity.Roles.First(), _authenticator.CurrentIdentity.Number!)).ConfigureAwait(false);
        return result.Success
            ? request.CreateResponse(HttpStatusCode.OK)
            : request.CreateResponse(HttpStatusCode.BadRequest);
    }
}
