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
using System.Text;
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
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
    private readonly IIncomingMessageClient _incomingMessageClient;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<IncomingMessageReceiver> _logger;

    public IncomingMessageReceiver(
        ILogger<IncomingMessageReceiver> logger,
        IIncomingMessageClient incomingMessageClient,
        IAuditLogger auditLogger)
    {
        _logger = logger;
        _incomingMessageClient = incomingMessageClient;
        _auditLogger = auditLogger;
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
        var cancellationToken = request.GetCancellationToken(hostCancellationToken);

        using var seekingStreamFromBody = await request.CreateSeekingStreamFromBodyAsync().ConfigureAwait(false);
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
            return request.CreateResponse(HttpStatusCode.NotFound);

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

        return await CreateResponseAsync(request, httpStatusCode, responseMessage).ConfigureAwait(false);
    }

    private static async Task<HttpResponseData> CreateResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        ResponseMessage responseMessage)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteStringAsync(responseMessage.MessageBody, Encoding.UTF8).ConfigureAwait(false);
        return response;
    }

    private static AuditLogEntityType? GetAffectedEntityType(IncomingDocumentType? incomingDocumentType)
    {
        if (incomingDocumentType == null) return null;

        var entityTypeMapping = new Dictionary<IncomingDocumentType, AuditLogEntityType>
        {
            { IncomingDocumentType.RequestAggregatedMeasureData, AuditLogEntityType.RequestAggregatedMeasureDataProcess },
            { IncomingDocumentType.B2CRequestAggregatedMeasureData, AuditLogEntityType.RequestAggregatedMeasureDataProcess },
            { IncomingDocumentType.RequestWholesaleSettlement, AuditLogEntityType.RequestWholesaleServicesProcess },
            { IncomingDocumentType.B2CRequestWholesaleSettlement, AuditLogEntityType.RequestWholesaleServicesProcess },
        };

        entityTypeMapping.TryGetValue(incomingDocumentType, out var affectedEntityType);
        return affectedEntityType;
    }

    private static AuditLogActivity GetAffectedEntityType(AuditLogEntityType? affectedEntityType)
    {
        var auditLogActivity = affectedEntityType is null
            ? AuditLogActivity.RequestInvalidCalculationTypeResults
            : AuditLogActivity.RequestCalculationResults;
        return auditLogActivity;
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

        var auditLogActivity = GetAffectedEntityType(affectedEntityType);
        var incomingMessage = await new StreamReader(incomingMarketMessageStream.Stream).ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await _auditLogger.LogWithCommitAsync(
                logId: AuditLogId.New(),
                activity: auditLogActivity,
                activityOrigin: request.Url.ToString(),
                activityPayload: incomingMessage,
                affectedEntityType: affectedEntityType,
                affectedEntityKey: null)
            .ConfigureAwait(false);
    }
}
