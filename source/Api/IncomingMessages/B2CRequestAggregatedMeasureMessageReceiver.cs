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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Api.Common;
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Domain.ArchivedMessages;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.Edi.Requests;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.Api.IncomingMessages;

public class B2CRequestAggregatedMeasureMessageReceiver
{
    private readonly ICorrelationContext _correlationContext;
    private readonly IArchivedMessageRepository _messageArchive;
    private readonly B2BContext _dbContext;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IMediator _mediator;

    public B2CRequestAggregatedMeasureMessageReceiver(
        ICorrelationContext correlationContext,
        IArchivedMessageRepository messageArchive,
        B2BContext dbContext,
        ISystemDateTimeProvider systemDateTimeProvider,
        IMediator mediator)
        {
        _correlationContext = correlationContext;
        _messageArchive = messageArchive;
        _dbContext = dbContext;
        _systemDateTimeProvider = systemDateTimeProvider;
        _mediator = mediator;
        }

    [Function(nameof(RequestAggregatedMeasureMessageReceiver))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData request,
        CancellationToken hostCancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var cancellationToken = request.GetCancellationToken(hostCancellationToken);

        var requestAggregatedMeasureData = RequestAggregatedMeasureData.Parser.ParseFrom(request.Body);
        await SaveArchivedMessageAsync(requestAggregatedMeasureData, request.Body, cancellationToken).ConfigureAwait(false);
        var marketMessage = RequestAggregatedMeasureDocumentFactory.Created(requestAggregatedMeasureData);

        var result = await _mediator
            .Send(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage), cancellationToken).ConfigureAwait(false);

        var httpStatusCode = result.Success ? HttpStatusCode.Accepted : HttpStatusCode.BadRequest;
        return CreateResponse(request, httpStatusCode);
    }

    private HttpResponseData CreateResponse(
        HttpRequestData request, HttpStatusCode statusCode)
    {
        var response = request.CreateResponse(statusCode);
        response.Headers.Add("CorrelationId", _correlationContext.Id);
        return response;
    }

    private async Task SaveArchivedMessageAsync(RequestAggregatedMeasureData requestAggregatedMeasureData, Stream document,  CancellationToken hostCancellationToken)
    {
        _messageArchive.Add(new ArchivedMessage(
            Guid.NewGuid().ToString(),
            requestAggregatedMeasureData.MessageId,
            IncomingDocumentType.RequestAggregatedMeasureData.Name,
            requestAggregatedMeasureData.SenderId,
            requestAggregatedMeasureData.ReceiverId,
            _systemDateTimeProvider.Now(),
            requestAggregatedMeasureData.BusinessReason,
            document));
        await _dbContext.SaveChangesAsync(hostCancellationToken).ConfigureAwait(false);
    }
}
