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

using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.EDI.Api.Common;
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Correlation;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Api.IncomingMessages;

public class IncomingMessageReceiver
{
    private readonly ICorrelationContext _correlationContext;
    private readonly IFeatureFlagManager _featureFlagManager;
    private readonly IIncomingMessageClient _incomingMessageClient;
    private readonly ILogger<IncomingMessageReceiver> _logger;

    public IncomingMessageReceiver(
        ILogger<IncomingMessageReceiver> logger,
        IIncomingMessageClient incomingMessageClient,
        ICorrelationContext correlationContext,
        IFeatureFlagManager featureFlagManager)
    {
        _logger = logger;
        _incomingMessageClient = incomingMessageClient;
        _correlationContext = correlationContext;
        _featureFlagManager = featureFlagManager;
    }

    [Function(nameof(IncomingMessageReceiver))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "incomingMessages/{incomingDocumentTypeName}")]
        HttpRequestData request,
        string? incomingDocumentTypeName,
        CancellationToken hostCancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cancellationToken = request.GetCancellationToken(hostCancellationToken);
        var contentType = request.Headers.TryGetContentType();
        if (contentType is null)
        {
            _logger.LogInformation(
                "Could not get Content-Type from request header.");
            return await request.CreateMissingContentTypeResponseAsync(cancellationToken).ConfigureAwait(false);
        }

        var documentFormat = DocumentFormatParser.ParseFromContentTypeHeaderValue(contentType);
        if (documentFormat is null)
        {
            _logger.LogInformation(
                "Could not parse desired document format from Content-Type header value: {ContentType}.",
                contentType);
            return await request.CreateInvalidContentTypeResponseAsync(cancellationToken).ConfigureAwait(false);
        }

        var incomingDocumentType = EnumerationType.FromName<IncomingDocumentType>(incomingDocumentTypeName ?? "RequestAggregatedMeasureData");
        if (incomingDocumentType == IncomingDocumentType.RequestWholesaleSettlement)
        {
            if (!await _featureFlagManager.UseRequestWholesaleSettlementReceiver.ConfigureAwait(false))
            {
                return request.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        if (incomingDocumentType == IncomingDocumentType.RequestAggregatedMeasureData)
        {
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

        return request.CreateResponse(HttpStatusCode.BadRequest);
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
