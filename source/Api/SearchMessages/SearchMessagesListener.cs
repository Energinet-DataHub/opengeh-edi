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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Application.SearchMessages;
using Infrastructure.Configuration.Serialization;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using NodaTime;

namespace Api.SearchMessages;

public class SearchMessagesListener
{
    private readonly IMediator _mediator;
    private readonly ISerializer _serializer;

    public SearchMessagesListener(IMediator mediator, ISerializer serializer)
    {
        _mediator = mediator;
        _serializer = serializer;
    }

    [Function("messages")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")]
        HttpRequestData request,
        FunctionContext executionContext,
        CancellationToken hostCancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var cancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                hostCancellationToken,
                request.FunctionContext.CancellationToken);

        var cancellationToken = cancellationTokenSource.Token;

        var result = await _mediator.Send(new GetMessagesQuery(), cancellationToken).ConfigureAwait(false);

        return await SearchResultResponseAsync(request, result, cancellationToken).ConfigureAwait(false);
    }

    [Function("ArchivedMessages")]
    public async Task<HttpResponseData> SearchArchivedMessagesAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData request,
        FunctionContext executionContext,
        CancellationToken hostCancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var cancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                hostCancellationToken,
                request.FunctionContext.CancellationToken);

        var cancellationToken = cancellationTokenSource.Token;

        if (request.Body == Stream.Null)
        {
            return request.CreateResponse(HttpStatusCode.BadRequest);
        }

        var searchCriteria = await _serializer
            .DeserializeAsync<SearchArchivedMessages>(request.Body, cancellationToken)
            .ConfigureAwait(false);

        var query = new GetMessagesQuery
        {
            CreationPeriod = searchCriteria?.CreatedDuringPeriod is not null
                ? new Application.SearchMessages.MessageCreationPeriod(
                    searchCriteria.CreatedDuringPeriod.Start,
                    searchCriteria.CreatedDuringPeriod.End)
                : null,
            MessageId = searchCriteria?.MessageId,
            SenderNumber = searchCriteria?.SenderNumber,
            ReceiverNumber = searchCriteria?.ReceiverNumber,
            DocumentTypes = searchCriteria?.DocumentTypes,
            ProcessTypes = searchCriteria?.ProcessTypes,
        };

        var result = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

        return await SearchResultResponseAsync(request, result, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseData> SearchResultResponseAsync(
        HttpRequestData request,
        MessageSearchResult result,
        CancellationToken cancellationToken)
    {
        var responseBody = _serializer.Serialize(result.Messages);
        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync(responseBody, cancellationToken).ConfigureAwait(false);
        response.Headers.Add("content-type", "application/json");
        return response;
    }
}

public record SearchArchivedMessages(
    MessageCreationPeriod? CreatedDuringPeriod,
    Guid? MessageId,
    string? SenderNumber,
    string? ReceiverNumber,
    IReadOnlyList<string>? DocumentTypes,
    IReadOnlyList<string>? ProcessTypes);

public record MessageCreationPeriod(Instant Start, Instant End);
