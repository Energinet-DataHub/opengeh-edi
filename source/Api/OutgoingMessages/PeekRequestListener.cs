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
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Api.Common;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Domain;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using IncomingMessages.Infrastructure;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Api.OutgoingMessages;

public class PeekRequestListener
{
    private readonly AuthenticatedActor _authenticatedActor;
    private readonly ILogger<PeekRequestListener> _logger;
    private readonly IMediator _mediator;

    public PeekRequestListener(
        AuthenticatedActor authenticatedActor,
        ILogger<PeekRequestListener> logger,
        IMediator mediator)
    {
        _authenticatedActor = authenticatedActor;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator;
    }

    [Function("PeekRequestListener")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "peek/{messageCategory}")]
        HttpRequestData request,
        FunctionContext executionContext,
        string? messageCategory)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contentType = request.Headers.GetContentType();
        var desiredDocumentFormat = DocumentFormatParser.ParseFromContentTypeHeaderValue(contentType);
        if (desiredDocumentFormat is null)
        {
            _logger.LogInformation(
                "Could not parse desired CIM format from Content-Type header value: {ContentType}", contentType);
            return request.CreateResponse(HttpStatusCode.UnsupportedMediaType);
        }

        var parsedMessageCategory = messageCategory != null && desiredDocumentFormat != DocumentFormat.Ebix
            ? EnumerationType.FromName<MessageCategory>(messageCategory)
            : MessageCategory.None;

        var peekResult = await _mediator.Send(new PeekCommand(
            _authenticatedActor.CurrentActorIdentity.ActorNumber,
            parsedMessageCategory,
            _authenticatedActor.CurrentActorIdentity.MarketRole!,
            desiredDocumentFormat)).ConfigureAwait(false);

        var response = HttpResponseData.CreateResponse(request);
        if (peekResult.MessageId is null)
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
        response.Headers.Add("MessageId", peekResult.MessageId.ToString());
        response.StatusCode = HttpStatusCode.OK;
        return response;
    }
}
