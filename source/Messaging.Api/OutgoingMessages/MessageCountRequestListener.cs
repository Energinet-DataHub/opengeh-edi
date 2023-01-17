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

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration.Authentication;
using Messaging.Application.OutgoingMessages.MessageCount;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.OutgoingMessages;

public class MessageCountRequestListener
{
    private readonly IMediator _mediator;
    private readonly IMarketActorAuthenticator _marketActorAuthenticator;

    public MessageCountRequestListener(IMediator mediator, IMarketActorAuthenticator marketActorAuthenticator)
    {
        _mediator = mediator;
        _marketActorAuthenticator = marketActorAuthenticator;
    }

    [Function("MessageCountRequestListener")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "messagecount")]
        HttpRequestData request,
        FunctionContext executionContext)
    {
        var result = await _mediator.Send(
                new MessageCountRequest(_marketActorAuthenticator.CurrentIdentity.Number))
            .ConfigureAwait(false);

        var response = HttpResponseData.CreateResponse(request);
        response.Body = new MemoryStream(Encoding.UTF8.GetBytes(result.MessageCount.ToString(CultureInfo.InvariantCulture)));
        response.Headers.Add("content-type", "application/xml");
        response.StatusCode = HttpStatusCode.OK;
        return response;
    }
}
