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
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.EDI.B2BApi.Common;
using Energinet.DataHub.EDI.B2BApi.Extensions;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.OutgoingMessages;

public class PeekRequestListener
{
    private readonly AuthenticatedActor _authenticatedActor;
    private readonly ILogger<PeekRequestListener> _logger;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly IFeatureFlagManager _featureFlagManager;

    public PeekRequestListener(
        AuthenticatedActor authenticatedActor,
        ILogger<PeekRequestListener> logger,
        IOutgoingMessagesClient outgoingMessagesClient,
        IFeatureFlagManager featureFlagManager)
    {
        _authenticatedActor = authenticatedActor;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _outgoingMessagesClient = outgoingMessagesClient;
        _featureFlagManager = featureFlagManager;
    }

    [Function("PeekRequestListener")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "peek/{messageCategory}")]
        HttpRequestData request,
        FunctionContext executionContext,
        string? messageCategory,
        CancellationToken hostCancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var response = HttpResponseData.CreateResponse(request);
        if (!await _featureFlagManager.UsePeekMessagesAsync().ConfigureAwait(false))
        {
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

        var desiredDocumentFormat = DocumentFormatParser.ParseFromContentTypeHeaderValue(contentType);
        if (desiredDocumentFormat is null)
        {
            _logger.LogInformation(
                "Could not parse desired document format from Content-Type header value: {ContentType}.",
                contentType);
            return await request.CreateInvalidContentTypeResponseAsync(cancellationToken).ConfigureAwait(false);
        }

        var parsedMessageCategory = messageCategory != null && desiredDocumentFormat != DocumentFormat.Ebix
            ? EnumerationType.FromName<MessageCategory>(messageCategory)
            : MessageCategory.None;

        var peekResult = await _outgoingMessagesClient.PeekAndCommitAsync(
                new PeekRequestDto(
                    _authenticatedActor.CurrentActorIdentity.ActorNumber,
                    parsedMessageCategory,
                    _authenticatedActor.CurrentActorIdentity.MarketRole!,
                    desiredDocumentFormat),
                cancellationToken)
            .ConfigureAwait(false);

        if (peekResult.BundleId is null)
        {
            response.StatusCode = HttpStatusCode.NoContent;
            return response;
        }

        if (peekResult.Bundle == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return response;
        }

        response.Body = peekResult.Bundle;
        response.Headers.Add("content-type", contentType);
        response.Headers.Add("MessageId", peekResult.MessageId);
        response.StatusCode = HttpStatusCode.OK;
        return response;
    }
}
