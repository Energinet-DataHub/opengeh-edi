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
using Domain.Actors;
using Domain.ArchivedMessages;
using Domain.Documents;
using Infrastructure.Configuration.DataAccess;
using Infrastructure.IncomingMessages;
using Infrastructure.IncomingMessages.RequestAggregatedMeasureData;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Receiver = CimMessageAdapter.Messages.RequestAggregatedMeasureData.RequestAggregatedMeasureDataReceiver;

namespace Api.IncomingMessages;

public class RequestAggregatedMeasureMessageReceiver
{
    private readonly ILogger<RequestAggregatedMeasureMessageReceiver> _logger;
    private readonly IArchivedMessageRepository _messageArchive;
    private readonly B2BContext _dbContext;
    private readonly RequestAggregatedMeasureDataReceiver _messageReceiver;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly MessageParser _messageParser;
    private readonly ResponseFactory _responseFactory;
    private readonly ICorrelationContext _correlationContext;
    private readonly IMediator _mediator;

    public RequestAggregatedMeasureMessageReceiver(
        ILogger<RequestAggregatedMeasureMessageReceiver> logger,
        IArchivedMessageRepository messageArchive,
        B2BContext dbContext,
        Receiver messageReceiver,
        ISystemDateTimeProvider systemDateTimeProvider,
        MessageParser messageParser,
        ResponseFactory responseFactory,
        ICorrelationContext correlationContext,
        IMediator mediator)
        {
        _logger = logger;
        _messageArchive = messageArchive;
        _dbContext = dbContext;
        _messageReceiver = messageReceiver;
        _systemDateTimeProvider = systemDateTimeProvider;
        _messageParser = messageParser;
        _responseFactory = responseFactory;
        _correlationContext = correlationContext;
        _mediator = mediator;
        }

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
            var httpErrorStatusCode = HttpStatusCode.BadRequest;
            return CreateResponse(request, httpErrorStatusCode, _responseFactory.From(errorResult, cimFormat));
        }

        _messageArchive.Add(new ArchivedMessage(
            Guid.NewGuid().ToString(),
            messageHeader.MessageId,
            IncomingDocumentType.RequestAggregatedMeasureData,
            TryGetActorNumber(messageHeader.SenderId),
            TryGetActorNumber(messageHeader.ReceiverId),
            _systemDateTimeProvider.Now(),
            messageHeader.BusinessReason,
            request.Body));
        await _dbContext.SaveChangesAsync(hostCancellationToken).ConfigureAwait(false);

        var result = await _mediator
            .Send(new ReceiveAggregatedMeasureDataRequest(messageParserResult), cancellationToken).ConfigureAwait(false);

        var httpStatusCode = result.Success ? HttpStatusCode.Accepted : HttpStatusCode.BadRequest;
        return CreateResponse(request, httpStatusCode, _responseFactory.From(result, cimFormat));
    }

    private static ActorNumber? TryGetActorNumber(string messageHeaderSenderId)
    {
        try
        {
            return ActorNumber.Create(messageHeaderSenderId);
        }
#pragma warning disable CA1031
        catch
#pragma warning restore CA1031
        {
            return null;
        }
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
