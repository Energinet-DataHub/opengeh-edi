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
using Application.ArchivedMessages;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Api.DownloadArchivedMessages
{
    public partial class DownloadArchivedMessageListener
    {
        private readonly IMediator _mediator;

        public DownloadArchivedMessageListener(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Function("ArchivedMessages")]
        public async Task<HttpResponseData> GetDocumentAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{id:Guid}/document")]
        HttpRequestData request,
        Guid id,
        FunctionContext executionContext,
        CancellationToken hostCancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            using var cancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    hostCancellationToken,
                    request.FunctionContext.CancellationToken);

            var cancellationToken = cancellationTokenSource.Token;

            var query = new GetArchivedMessageDocumentQuery(id);

            var result = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

            var response = HttpResponseData.CreateResponse(request);
            if (result is null)
            {
                response.StatusCode = HttpStatusCode.NoContent;
                return response;
            }

            response.Body = result;
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
    }
}
