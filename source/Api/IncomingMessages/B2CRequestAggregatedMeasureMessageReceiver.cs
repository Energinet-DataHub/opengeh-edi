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
using Energinet.DataHub.EDI.Api.Common;
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.Edi.Requests;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.Api.IncomingMessages;

public class B2CRequestAggregatedMeasureMessageReceiver
{
    private readonly ICorrelationContext _correlationContext;
    private readonly IMediator _mediator;

    public B2CRequestAggregatedMeasureMessageReceiver(
        ICorrelationContext correlationContext,
        IMediator mediator)
        {
        _correlationContext = correlationContext;
        _mediator = mediator;
        }

    [Function(nameof(B2CRequestAggregatedMeasureMessageReceiver))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData request,
        CancellationToken hostCancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var cancellationToken = request.GetCancellationToken(hostCancellationToken);

        var requestAggregatedMeasureData = RequestAggregatedMeasureData.Parser.ParseFrom(request.Body);
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
}
