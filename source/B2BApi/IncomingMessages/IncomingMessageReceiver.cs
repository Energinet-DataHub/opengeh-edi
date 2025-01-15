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

using System.Diagnostics;
using System.Net;
using System.Text;
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.B2BApi.Common;
using Energinet.DataHub.EDI.B2BApi.Extensions;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.IncomingMessages;

public class IncomingMessageReceiver
{
    private readonly IIncomingMessageClient _incomingMessageClient;
    private readonly IAuditLogger _auditLogger;
    private readonly IFeatureFlagManager _featureFlagManager;
    private readonly ILogger<IncomingMessageReceiver> _logger;

    public IncomingMessageReceiver(
        ILogger<IncomingMessageReceiver> logger,
        IIncomingMessageClient incomingMessageClient,
        IAuditLogger auditLogger,
        IFeatureFlagManager featureFlagManager)
    {
        _logger = logger;
        _incomingMessageClient = incomingMessageClient;
        _auditLogger = auditLogger;
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
        var stopwatch = Stopwatch.StartNew();
        var cancellationToken = request.GetCancellationToken(hostCancellationToken);

        if (incomingDocumentTypeName != null &&
            incomingDocumentTypeName.Equals(IncomingDocumentType.NotifyValidatedMeasureData.Name, StringComparison.OrdinalIgnoreCase)
            && !await _featureFlagManager.ReceiveMeteredDataForMeasurementPointsAsync().ConfigureAwait(false))
        {
            /*
             * The HTTP 403 Forbidden client error response status code indicates that the server understood the request
             * but refused to process it. This status is similar to 401, except that for 403 Forbidden responses,
             * authenticating or re-authenticating makes no difference. The request failure is tied to application logic,
             * such as insufficient permissions to a resource or action.
             */
            return request.CreateResponse(HttpStatusCode.Forbidden);
        }

        using var seekingStreamFromBody = await request.CreateSeekingStreamFromBodyAsync(cancellationToken).ConfigureAwait(false);
        var incomingMarketMessageStream = new IncomingMarketMessageStream(seekingStreamFromBody);
        await AuditLogAsync(request, incomingDocumentTypeName, incomingMarketMessageStream, cancellationToken).ConfigureAwait(false);

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
        if (incomingDocumentType == null)
        {
            var responseData = request.CreateResponse(HttpStatusCode.NotFound);
            responseData.Headers.Add("Content-Type", $"{documentFormat.GetContentType()}; charset=utf-8");
            return responseData;
        }

        var responseMessage = await _incomingMessageClient
            .ReceiveIncomingMarketMessageAsync(
                incomingMarketMessageStream,
                incomingDocumentFormat: documentFormat,
                incomingDocumentType,
                responseDocumentFormat: documentFormat,
                cancellationToken)
            .ConfigureAwait(false);

        var httpStatusCode = responseMessage.IsErrorResponse
            ? HttpStatusCode.BadRequest
            : HttpStatusCode.Accepted;

        var httpResponseData = await CreateResponseAsync(request, httpStatusCode, documentFormat, responseMessage)
            .ConfigureAwait(false);

        stopwatch.Stop();
        _logger.LogInformation($"IncomingMessage Execution time: {stopwatch.ElapsedMilliseconds} ms");

        return httpResponseData;
    }

    private static async Task<HttpResponseData> CreateResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        DocumentFormat documentFormat,
        ResponseMessage responseMessage)
    {
        var response = request.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", $"{documentFormat.GetContentType()}; charset=utf-8");
        await response.WriteStringAsync(responseMessage.MessageBody, Encoding.UTF8).ConfigureAwait(false);

        return response;
    }

    private static AuditLogEntityType? GetAffectedEntityType(IncomingDocumentType? incomingDocumentType)
    {
        if (incomingDocumentType == null) return null;

        var entityTypeMapping = new Dictionary<IncomingDocumentType, AuditLogEntityType>
        {
            { IncomingDocumentType.RequestAggregatedMeasureData, AuditLogEntityType.RequestAggregatedMeasureData },
            { IncomingDocumentType.RequestWholesaleSettlement, AuditLogEntityType.RequestWholesaleServices },
            { IncomingDocumentType.NotifyValidatedMeasureData, AuditLogEntityType.MeteredDataForMeteringPointReceived },
        };

        entityTypeMapping.TryGetValue(incomingDocumentType, out var affectedEntityType);
        return affectedEntityType;
    }

    private async Task AuditLogAsync(
        HttpRequestData request,
        string? incomingDocumentTypeName,
        IncomingMarketMessageStream incomingMarketMessageStream,
        CancellationToken cancellationToken)
    {
        AuditLogEntityType? affectedEntityType;
        try
        {
            var incomingDocumentType = IncomingDocumentType.FromName(incomingDocumentTypeName);
            affectedEntityType = GetAffectedEntityType(incomingDocumentType);
        }
        catch (InvalidOperationException)
        {
            // If the incomingDocumentTypeName is not a valid IncomingDocumentType, we do not log a affectedEntityType
            affectedEntityType = null;
        }

        // Do not log the message if it is a MeteredDataForMeteringPointReceived message
        if (affectedEntityType == AuditLogEntityType.MeteredDataForMeteringPointReceived) return;

        var incomingMessage = await new StreamReader(incomingMarketMessageStream.Stream).ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await _auditLogger.LogWithCommitAsync(
                logId: AuditLogId.New(),
                activity: AuditLogActivity.RequestCalculationResults,
                activityOrigin: request.Url.ToString(),
                activityPayload: new { IncomingDocumentType = incomingDocumentTypeName, Message = incomingMessage },
                affectedEntityType: affectedEntityType,
                affectedEntityKey: null)
            .ConfigureAwait(false);
    }
}
