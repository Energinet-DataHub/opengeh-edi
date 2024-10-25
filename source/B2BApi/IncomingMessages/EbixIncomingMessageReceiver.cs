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

using System.Net;
using System.Text;
using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.EDI.B2BApi.Common;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.B2BApi.IncomingMessages;

public sealed class EbixIncomingMessageReceiver(
    IIncomingMessageClient incomingMessageClient,
    IFeatureFlagManager featureFlagManager)
{
    private readonly IIncomingMessageClient _incomingMessageClient = incomingMessageClient;
    private readonly IFeatureFlagManager _featureFlagManager = featureFlagManager;

    [Function(nameof(EbixIncomingMessageReceiver))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "incomingMessages/ebix")]
        HttpRequestData request,
        FunctionContext executionContext,
        CancellationToken hostCancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var cancellationToken = request.GetCancellationToken(hostCancellationToken);

        using var seekingStreamFromBody = await request
            .CreateSeekingStreamFromBodyAsync(cancellationToken)
            .ConfigureAwait(false);

        var incomingMarketMessageStream = new IncomingMarketMessageStream(seekingStreamFromBody);

        if (!await _featureFlagManager.ReceiveMeteredDataForMeasurementPointsAsync().ConfigureAwait(false))
        {
            /*
             * The HTTP 403 Forbidden client error response status code indicates that the server understood the request
             * but refused to process it. This status is similar to 401, except that for 403 Forbidden responses,
             * authenticating or re-authenticating makes no difference. The request failure is tied to application logic,
             * such as insufficient permissions to a resource or action.
             */
            return request.CreateResponse(HttpStatusCode.Forbidden);
        }

        var responseMessage = await _incomingMessageClient
            .ReceiveIncomingMarketMessageAsync(
                incomingMarketMessageStream,
                incomingDocumentFormat: DocumentFormat.Ebix,
                IncomingDocumentType.MeteredDataForMeasurementPoint,
                responseDocumentFormat: DocumentFormat.Ebix,
                cancellationToken)
            .ConfigureAwait(false);

        var httpStatusCode = responseMessage.IsErrorResponse
            ? HttpStatusCode.BadRequest
            : HttpStatusCode.Accepted;

        var httpResponseData = await CreateResponseAsync(request, httpStatusCode, responseMessage)
            .ConfigureAwait(false);

        return httpResponseData;
    }

    private static async Task<HttpResponseData> CreateResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        ResponseMessage responseMessage)
    {
        var response = request.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", $"{DocumentFormat.Ebix.GetContentType()}; charset=utf-8");
        await response.WriteStringAsync(responseMessage.MessageBody, Encoding.UTF8).ConfigureAwait(false);

        return response;
    }
}
