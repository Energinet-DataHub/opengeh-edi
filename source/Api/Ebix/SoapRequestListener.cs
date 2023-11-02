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
using Energinet.DataHub.EDI.Application.Configuration.Authentication;
using Energinet.DataHub.EDI.Application.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Api.Ebix
{
    public class SoapRequestListener
    {
        private readonly IMediator _mediator;
        private readonly IMarketActorAuthenticator _authenticator;
        private readonly ILogger<SoapRequestListener> _logger;

        public SoapRequestListener(IMediator mediator, IMarketActorAuthenticator authenticator, ILogger<SoapRequestListener> logger)
        {
            _mediator = mediator;
            _authenticator = authenticator;
            _logger = logger;
        }

        [Function("SoapDequeueMessage")]
        public async Task<HttpResponseData> DequeueMessageAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "dequeue/{messageId}"),]
            HttpRequestData request,
            FunctionContext executionContext,
            string messageId)
        {
            var result = await _mediator.Send(new DequeueCommand(messageId, _authenticator.CurrentIdentity.Roles.First(), _authenticator.CurrentIdentity.Number!)).ConfigureAwait(false);
            return result.Success
                ? request.CreateResponse(HttpStatusCode.OK)
                : request.CreateResponse(HttpStatusCode.BadRequest);
        }

        [Function("SoapPeekMessage")]
        public async Task<HttpResponseData> PeekMessageAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "peekMessage"),]
            HttpRequestData request,
            FunctionContext executionContext,
            string messageId)
        {
            ArgumentNullException.ThrowIfNull(request);

            var contentType = request.Headers.GetContentType();
            var desiredDocumentFormat = CimFormatParser.ParseFromContentTypeHeaderValue(contentType);
            if (desiredDocumentFormat is null)
            {
                _logger.LogInformation(
                    "Could not parse desired CIM format from Content-Type header value: {ContentType}", contentType);
                return request.CreateResponse(HttpStatusCode.UnsupportedMediaType);
            }

            if (desiredDocumentFormat != DocumentFormat.Ebix)
            {
                _logger.LogInformation(
                    "Desired format from Content-Type header value: {ContentType} is not Ebix", contentType);
                return request.CreateResponse(HttpStatusCode.UnsupportedMediaType);
            }

            var peekResult = await _mediator.Send(new PeekCommand(
                    _authenticator.CurrentIdentity.Number!,
                    MessageCategory.None,
                    _authenticator.CurrentIdentity.Roles.First(),
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

        // TODO - add functionality to validate incoming xmlContent against schemadefinitions and other requirements when working with incoming Ebix documents
        //[Function("SoapSoapMessage")]
        //public async Task<HttpResponseData> SendMessageAsync(
        //    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sendMessage"),]
        //    HttpRequestData request,
        //    FunctionContext executionContext,
        //    string xmlContent)
        //{
        //    return request.CreateResponsem(HttpStatusCode.NoContent);
        //}
    }
}
