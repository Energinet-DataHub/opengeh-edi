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
using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.EDI.B2BApi.Common;
using Energinet.DataHub.EDI.B2BApi.Extensions;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.IncomingMessages;

public class IncomingMessageReceiver
{
    private readonly IFeatureFlagManager _featureFlagManager;
    private readonly IIncomingMessageClient _incomingMessageClient;
    private readonly ILogger<IncomingMessageReceiver> _logger;

    public IncomingMessageReceiver(
        ILogger<IncomingMessageReceiver> logger,
        IIncomingMessageClient incomingMessageClient,
        IFeatureFlagManager featureFlagManager)
    {
        _logger = logger;
        _incomingMessageClient = incomingMessageClient;
        _featureFlagManager = featureFlagManager;
    }

    [Function(nameof(IncomingMessageReceiver))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "incomingMessages/{incomingDocumentTypeName}")]
        HttpRequestData request,
        FunctionContext executionContext,
        string? incomingDocumentTypeName,
        CancellationToken hostCancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!await _featureFlagManager.UseRequestMessagesAsync().ConfigureAwait(false))
        {
            var response = HttpResponseData.CreateResponse(request);
            response.StatusCode = HttpStatusCode.NotFound;
            return response;
        }

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

        var incomingDocumentType = IncomingDocumentType.FromName(incomingDocumentTypeName);
        if (incomingDocumentType == null) return request.CreateResponse(HttpStatusCode.NotFound);

        if (incomingDocumentType == IncomingDocumentType.RequestWholesaleSettlement
            && !await _featureFlagManager.UseRequestWholesaleSettlementReceiverAsync().ConfigureAwait(false))
        {
            return request.CreateResponse(HttpStatusCode.NotFound);
        }

        var responseMessage = await _incomingMessageClient
            .ReceiveIncomingMarketMessageAsync(
                new IncomingMarketMessageStream(request.Body),
                incomingDocumentFormat: documentFormat,
                incomingDocumentType,
                responseDocumentFormat: documentFormat,
                cancellationToken)
            .ConfigureAwait(false);

        var httpStatusCode = responseMessage.IsErrorResponse ? HttpStatusCode.BadRequest : HttpStatusCode.Accepted;

        return CreateResponse(request, httpStatusCode, responseMessage);
    }

    private static HttpResponseData CreateResponse(
        HttpRequestData request,
        HttpStatusCode statusCode,
        ResponseMessage responseMessage)
    {
        var response = request.CreateResponse(statusCode);
        response.WriteString(responseMessage.MessageBody, Encoding.UTF8);
        return response;
    }
}
