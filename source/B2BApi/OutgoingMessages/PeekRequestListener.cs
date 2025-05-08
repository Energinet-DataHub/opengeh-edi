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
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.B2BApi.Common;
using Energinet.DataHub.EDI.B2BApi.Extensions;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureManagement;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Energinet.DataHub.EDI.B2BApi.OutgoingMessages;

public class PeekRequestListener
{
    private readonly AuthenticatedActor _authenticatedActor;
    private readonly ILogger<PeekRequestListener> _logger;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly IFeatureManager _featureManager;
    private readonly IAuditLogger _auditLogger;

    public PeekRequestListener(
        AuthenticatedActor authenticatedActor,
        ILogger<PeekRequestListener> logger,
        IOutgoingMessagesClient outgoingMessagesClient,
        IFeatureManager featureManager,
        IAuditLogger auditLogger)
    {
        _authenticatedActor = authenticatedActor;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _outgoingMessagesClient = outgoingMessagesClient;
        _featureManager = featureManager;
        _auditLogger = auditLogger;
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
        var cancellationToken = request.GetCancellationToken(hostCancellationToken);
        await _auditLogger.LogWithCommitAsync(
                logId: AuditLogId.New(),
                activity: AuditLogActivity.Peek,
                activityOrigin: request.Url.ToString(),
                activityPayload: messageCategory,
                affectedEntityType: AuditLogEntityType.Bundle,
                affectedEntityKey: null,
                cancellationToken)
            .ConfigureAwait(false);

        if (HttpContextHasBeenDisposed(request))
        {
            throw new OperationCanceledException("Request has been disposed before accessing headers.");
        }

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

        if (!await _featureManager.UsePeekMessagesAsync().ConfigureAwait(false))
        {
            var noContentResponse = HttpResponseData.CreateResponse(request);
            noContentResponse.Headers.Add("Content-Type", $"{desiredDocumentFormat.GetContentType()}; charset=utf-8");
            noContentResponse.StatusCode = HttpStatusCode.NoContent;
            return noContentResponse;
        }

        var parsedMessageCategory = messageCategory != null && desiredDocumentFormat != DocumentFormat.Ebix
            ? messageCategory.Equals("timeseries", StringComparison.InvariantCultureIgnoreCase)
                ? MessageCategory.MeasureData
                : EnumerationType.FromName<MessageCategory>(messageCategory)
            : MessageCategory.None;

        var peekResult = await _outgoingMessagesClient.PeekAndCommitAsync(
                new PeekRequestDto(
                    _authenticatedActor.CurrentActorIdentity.ActorNumber,
                    parsedMessageCategory,
                    _authenticatedActor.CurrentActorIdentity.ActorRole,
                    desiredDocumentFormat),
                cancellationToken)
            .ConfigureAwait(false);

        if (HttpContextHasBeenDisposed(request))
        {
            throw new OperationCanceledException("Request has been disposed before accessing headers.");
        }

        var response = HttpResponseData.CreateResponse(request);
        response.Headers.Add("Content-Type", $"{desiredDocumentFormat.GetContentType()}; charset=utf-8");

        if (peekResult is null)
        {
            response.StatusCode = HttpStatusCode.NoContent;
            return response;
        }

        // Must set status code before writing to body
        response.StatusCode = HttpStatusCode.OK;
        response.Headers.Add("MessageId", peekResult.MessageId.Value);

        await peekResult.Bundle.CopyToAsync(response.Body, cancellationToken).ConfigureAwait(false);

        return response;
    }

    private static bool HttpContextHasBeenDisposed(HttpRequestData request)
    {
        try
        {
            return request.FunctionContext.GetHttpContext()?.Features is null;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }
}
