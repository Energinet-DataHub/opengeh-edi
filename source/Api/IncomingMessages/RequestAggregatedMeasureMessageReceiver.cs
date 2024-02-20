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
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Api.Common;
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using IncomingMessages.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Api.IncomingMessages;

public class RequestAggregatedMeasureMessageReceiver
{
    private readonly ICorrelationContext _correlationContext;
    private readonly IIncomingMessageClient _incomingMessageClient;
    private readonly ILogger<RequestAggregatedMeasureMessageReceiver> _logger;

    public RequestAggregatedMeasureMessageReceiver(
        ILogger<RequestAggregatedMeasureMessageReceiver> logger,
        IIncomingMessageClient incomingMessageClient,
        ICorrelationContext correlationContext)
    {
        _logger = logger;
        _incomingMessageClient = incomingMessageClient;
        _correlationContext = correlationContext;
    }

    [Function(nameof(RequestAggregatedMeasureMessageReceiver))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData request,
        CancellationToken hostCancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cancellationToken = request.GetCancellationToken(hostCancellationToken);
        var contentType = request.Headers.TryGetContentType();
        if (contentType is null)
        {
            _logger.LogInformation(
                "Could not get Content-Type from request header.");
            return request.CreateResponse(HttpStatusCode.UnsupportedMediaType);
        }

        var documentFormat = DocumentFormatParser.ParseFromContentTypeHeaderValue(contentType);
        if (documentFormat is null)
        {
            _logger.LogInformation(
                "Could not parse desired document format from Content-Type header value: {ContentType}," +
                        "supported values are: application/xml, application/json, application/ebix",
                contentType);
            return request.CreateResponse(HttpStatusCode.UnsupportedMediaType);
        }

        var responseMessage = await _incomingMessageClient
            .RegisterAndSendAsync(
                new IncomingMessageStream(request.Body),
                documentFormat,
                IncomingDocumentType.RequestAggregatedMeasureData,
                cancellationToken)
            .ConfigureAwait(false);

        if (responseMessage.IsErrorResponse)
        {
            var httpErrorStatusCode = HttpStatusCode.BadRequest;
            return CreateResponse(request, httpErrorStatusCode, responseMessage);
        }

        var httpStatusCode = !responseMessage.IsErrorResponse ? HttpStatusCode.Accepted : HttpStatusCode.BadRequest;
        return CreateResponse(request, httpStatusCode, responseMessage);
    }

    private HttpResponseData CreateResponse(
        HttpRequestData request,
        HttpStatusCode statusCode,
        ResponseMessage responseMessage)
    {
        var response = request.CreateResponse(statusCode);
        response.WriteString(responseMessage.MessageBody, Encoding.UTF8);
        response.Headers.Add("CorrelationId", _correlationContext.Id);
        return response;
    }
}
