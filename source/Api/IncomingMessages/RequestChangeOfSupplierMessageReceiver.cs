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
using Api.Common;
using Application.Configuration;
using CimMessageAdapter.Messages.RequestChangeOfSupplier;
using CimMessageAdapter.Response;
using Infrastructure.IncomingMessages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Api.IncomingMessages
{
    public class RequestChangeOfSupplierMessageReceiver
    {
        private readonly ILogger<RequestChangeOfSupplierMessageReceiver> _logger;
        private readonly ICorrelationContext _correlationContext;
        private readonly RequestChangeOfSupplierReceiver _messageReceiver;
        private readonly ResponseFactory _responseFactory;
        private readonly MessageParser _messageParser;

        public RequestChangeOfSupplierMessageReceiver(
            ILogger<RequestChangeOfSupplierMessageReceiver> logger,
            ICorrelationContext correlationContext,
            RequestChangeOfSupplierReceiver messageReceiver,
            ResponseFactory responseFactory,
            MessageParser messageParser)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
            _messageReceiver = messageReceiver ?? throw new ArgumentNullException(nameof(messageReceiver));
            _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
            _messageParser = messageParser ?? throw new ArgumentNullException(nameof(messageParser));
        }

        [Function("RequestChangeOfSupplier")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequestData request,
            CancellationToken hostCancellationToken)
        {
            _logger.LogInformation($"Received {nameof(RequestChangeOfSupplierMessageReceiver)} request");

            if (request == null) throw new ArgumentNullException(nameof(request));

            using var cancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    hostCancellationToken,
                    request.FunctionContext.CancellationToken);

            var cancellationToken = cancellationTokenSource.Token;

            var contentType = request.Headers.GetContentType();
            var cimFormat = CimFormatParser.ParseFromContentTypeHeaderValue(contentType);
            if (cimFormat is null)
            {
                _logger.LogInformation(
                    $"Could not parse desired CIM format from Content-Type header value: {contentType}");
                return request.CreateResponse(HttpStatusCode.UnsupportedMediaType);
            }

            var messageParserResult = await _messageParser.ParseAsync(request.Body, cimFormat, cancellationToken).ConfigureAwait(false);
            var result = await _messageReceiver.ReceiveAsync(messageParserResult, cancellationToken)
                .ConfigureAwait(false);

            var httpStatusCode = result.Success ? HttpStatusCode.Accepted : HttpStatusCode.BadRequest;
            return CreateResponse(request, httpStatusCode, _responseFactory.From(result, cimFormat));
        }

        private HttpResponseData CreateResponse(
            HttpRequestData request, HttpStatusCode statusCode, ResponseMessage responseMessage)
        {
            var response = request.CreateResponse(statusCode);
            response.WriteString(responseMessage.MessageBody, Encoding.UTF8);
            response.Headers.Add("CorrelationId", _correlationContext.Id);
            return response;
        }
    }
}
