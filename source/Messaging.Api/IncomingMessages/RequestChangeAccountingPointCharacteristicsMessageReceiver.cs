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
using Messaging.Infrastructure.IncomingMessages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.IncomingMessages;

public class RequestChangeAccountingPointCharacteristicsMessageReceiver
{
    private readonly ILogger<RequestChangeAccountingPointCharacteristicsMessageReceiver> _logger;

    public RequestChangeAccountingPointCharacteristicsMessageReceiver(
        ILogger<RequestChangeAccountingPointCharacteristicsMessageReceiver> logger)
    {
        _logger = logger;
    }

    [Function("RequestChangeAccountingPointCharacteristics")]
    public HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData request)
    {
        _logger.LogInformation($"Received {nameof(RequestChangeAccountingPointCharacteristicsMessageReceiver)} request");
        if (request == null) throw new ArgumentNullException(nameof(request));

        var contentType = GetContentType(request.Headers);
        var cimFormat = CimFormatParser.ParseFromContentTypeHeaderValue(contentType);
        if (cimFormat is null)
        {
            _logger.LogInformation($"Could not parse desired CIM format from Content-Type header value: {contentType}");
            return request.CreateResponse(HttpStatusCode.UnsupportedMediaType);
        }

        return request.CreateResponse(HttpStatusCode.Accepted);
    }

    private static string GetContentType(HttpHeaders headers)
    {
        var contentHeader = headers.GetValues("Content-Type").FirstOrDefault();
        if (contentHeader == null) throw new InvalidOperationException("No Content-Type found in request headers");
        return contentHeader;
    }
}
