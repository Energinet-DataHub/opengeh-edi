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
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration.Authentication;
using Messaging.Application.OutgoingMessages.Peek;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Domain.SeedWork;
using Messaging.Infrastructure.IncomingMessages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Messaging.Api.OutgoingMessages;

public class PeekRequestListener
{
    private readonly IMediator _mediator;
    private readonly IMarketActorAuthenticator _authenticator;

    public PeekRequestListener(IMediator mediator, IMarketActorAuthenticator authenticator)
    {
        _mediator = mediator;
        _authenticator = authenticator;
    }

    [Function("PeekRequestListener")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "peek/{messageCategory}")]
        HttpRequestData request,
        FunctionContext executionContext,
        string messageCategory)
    {
        var result = await _mediator.Send(
            new PeekRequest(
                _authenticator.CurrentIdentity.Number,
                EnumerationType.FromName<MessageCategory>(messageCategory),
                _authenticator.CurrentIdentity.Role!))
            .ConfigureAwait(false);

        var response = HttpResponseData.CreateResponse(request);
        if (result.Bundle is null)
        {
            response.StatusCode = HttpStatusCode.NoContent;
            return response;
        }

        response.Body = result.Bundle;
        response.Headers.Add("content-type", "application/xml");
        response.StatusCode = HttpStatusCode.OK;
        return response;
    }
}
