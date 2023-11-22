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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Api.Common;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.Edi.Requests;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.Api.IncomingMessages;

//TODO: Remove this endpoint and refactor the RequestAggregatedMeasureDataController
public class B2CRequestAggregatedMeasureMessageReceiver
{
    private readonly IIncomingRequestAggregatedMeasuredParser _incomingRequestAggregatedMeasuredParser;

    public B2CRequestAggregatedMeasureMessageReceiver(IIncomingRequestAggregatedMeasuredParser incomingRequestAggregatedMeasuredParser)
        {
        _incomingRequestAggregatedMeasuredParser = incomingRequestAggregatedMeasuredParser;
        }

    [Function(nameof(B2CRequestAggregatedMeasureMessageReceiver))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData request,
        CancellationToken hostCancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var cancellationToken = request.GetCancellationToken(hostCancellationToken);

        var responseMessage = await _incomingRequestAggregatedMeasuredParser.ParseAsync(
            request.Body,
            DocumentFormat.Proto,
            cancellationToken,
            DocumentFormat.Json)
            .ConfigureAwait(false);

        var httpStatusCode = !responseMessage.IsErrorResponse ? HttpStatusCode.Accepted : HttpStatusCode.BadRequest;
        return CreateResponse(request, httpStatusCode, responseMessage);
    }

    private static HttpResponseData CreateResponse(
        HttpRequestData request, HttpStatusCode statusCode, ResponseMessage responseMessage)
    {
        var response = request.CreateResponse(statusCode);
        response.WriteString(responseMessage.MessageBody, Encoding.UTF8);
        return response;
    }
}
