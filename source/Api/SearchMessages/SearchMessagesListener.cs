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
        FunctionContext executionContext)
    {
        var result = await _mediator.Send(new GetMessagesQuery()).ConfigureAwait(false);

        return await SearchResultResponseAsync(request, result).ConfigureAwait(false);
    }

    [Function("ArchivedMessages")]
    public async Task<HttpResponseData> SearchArchivedMessagesAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Body == Stream.Null)
        {
            return request.CreateResponse(HttpStatusCode.BadRequest);
        }

        var searchCriteria = await _serializer.DeserializeAsync(request.Body, typeof(SearchArchivedMessages))
            .ConfigureAwait(false) as SearchArchivedMessages;

        var query = new GetMessagesQuery
        {
            CreationPeriod = searchCriteria?.CreatedDuringPeriod is not null
                ? new Application.SearchMessages.MessageCreationPeriod(
                    searchCriteria.CreatedDuringPeriod.Start,
                    searchCriteria.CreatedDuringPeriod.End)
                : null,
        };

        var result = await _mediator.Send(query).ConfigureAwait(false);

        return await SearchResultResponseAsync(request, result).ConfigureAwait(false);
    }

    private async Task<HttpResponseData> SearchResultResponseAsync(HttpRequestData request, MessageSearchResult result)
    {
        var responseBody = _serializer.Serialize(result.Messages);
        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync(responseBody).ConfigureAwait(false);
        response.Headers.Add("content-type", "application/json");
        return response;
    }
}

public record SearchArchivedMessages(MessageCreationPeriod? CreatedDuringPeriod);

public record MessageCreationPeriod(Instant Start, Instant End);
