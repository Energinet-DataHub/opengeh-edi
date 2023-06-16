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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Api.Common;
using Application.Configuration;
using CimMessageAdapter.Messages;
using CimMessageAdapter.Messages.RequestAggregatedMeasureData;
using CimMessageAdapter.Response;
using CimMessageAdapter.ValidationErrors;
using Domain.Actors;
using Domain.ArchivedMessages;
using Domain.Documents;
using Domain.OutgoingMessages;
using Infrastructure.IncomingMessages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Receiver = CimMessageAdapter.Messages.RequestAggregatedMeasureData.RequestAggregatedMeasureDataReceiver;

namespace Api.IncomingMessages;

public class RequestAggregatedMeasureMessageReceiver
{
    private readonly ILogger<RequestAggregatedMeasureMessageReceiver> _logger;
    private readonly MessageParser _messageParser;
    private readonly Receiver _messageReceiver;
    private readonly ResponseFactory _responseFactory;
    private readonly ICorrelationContext _correlationContext;
    private readonly IArchivedMessageRepository _messageArchive;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public RequestAggregatedMeasureMessageReceiver(
        ILogger<RequestAggregatedMeasureMessageReceiver> logger,
        MessageParser messageParser,
        Receiver messageReceiver,
        ResponseFactory responseFactory,
        ICorrelationContext correlationContext,
        IArchivedMessageRepository messageArchive,
        ISystemDateTimeProvider systemDateTimeProvider)
    {
        _logger = logger;
        _messageParser = messageParser;
        _messageReceiver = messageReceiver;
        _responseFactory = responseFactory;
        _correlationContext = correlationContext;
        _messageArchive = messageArchive;
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    //TODO: refactor functions to use nameof for function name
    [Function(nameof(RequestAggregatedMeasureMessageReceiver))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData request,
        CancellationToken hostCancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

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
                "Could not parse desired CIM format from Content-Type header value: {ContentType}", contentType);
            return request.CreateResponse(HttpStatusCode.UnsupportedMediaType);
        }

        var messageParserResult = await _messageParser.ParseAsync(request.Body, cimFormat, cancellationToken).ConfigureAwait(false);

        var messageHeader = messageParserResult.IncomingMarketDocument?.Header;
        if (messageHeader is null || messageParserResult.Errors.Any())
        {
            var errorResult = Result.Failure(messageParserResult.Errors.ToArray());
            var httpErrorStatusCode = messageParserResult.Errors.Any(x => x is MessageSizeExceeded) ? HttpStatusCode.RequestEntityTooLarge : HttpStatusCode.BadRequest;
            return CreateResponse(request, httpErrorStatusCode, _responseFactory.From(errorResult, cimFormat));
        }

        var timestamp = _systemDateTimeProvider.Now();
        _messageArchive.Add(new ArchivedMessage(
            messageHeader.MessageId,
            DocumentType.RequestAggregatedMeasureData,
            ActorNumber.Create(messageHeader.SenderId),
            ActorNumber.Create(messageHeader.ReceiverId),
            timestamp,
            BusinessReason.From(messageHeader.BusinessReason),
            request.Body));

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
