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

using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Api.Common;
using Application.Configuration.Authentication;
using Application.OutgoingMessages.Peek;
using Domain.OutgoingMessages.Peek;
using Domain.SeedWork;
using Infrastructure.IncomingMessages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Api.OutgoingMessages;

public class PeekRequestListener
{
    private readonly MessagePeeker _messagePeeker;
    private readonly IMarketActorAuthenticator _authenticator;
    private readonly ILogger<PeekRequestListener> _logger;

    public PeekRequestListener(MessagePeeker messagePeeker, IMarketActorAuthenticator authenticator, ILogger<PeekRequestListener> logger)
    {
        _messagePeeker = messagePeeker;
        _authenticator = authenticator;
        _logger = logger;
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
        ArgumentNullException.ThrowIfNull(request);

        var contentType = request.Headers.GetContentType();
        var desiredDocumentFormat = CimFormatParser.ParseFromContentTypeHeaderValue(contentType);
        if (desiredDocumentFormat is null)
        {
            _logger.LogInformation($"Could not parse desired CIM format from Content-Type header value: {contentType}");
            return request.CreateResponse(HttpStatusCode.UnsupportedMediaType);
        }

        var result = await _messagePeeker.PeekAsync(
                _authenticator.CurrentIdentity.Number,
                EnumerationType.FromName<MessageCategory>(messageCategory),
                desiredDocumentFormat)
            .ConfigureAwait(false);

        var response = HttpResponseData.CreateResponse(request);
        if (result.Bundle is null)
        {
            response.StatusCode = HttpStatusCode.NoContent;
            return response;
        }

        if (result.MessageId == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return response;
        }

        response.Body = result.Bundle;
        response.Headers.Add("content-type", "application/xml");
        response.Headers.Add("MessageId", result.MessageId.ToString());
        response.StatusCode = HttpStatusCode.OK;
        return response;
    }
}
