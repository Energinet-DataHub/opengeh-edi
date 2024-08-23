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
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.B2BApi.Common;

public static class HttpRequestDataExtensions
{
    public static async Task<MemoryStream> CreateSeekingStreamFromBodyAsync(this HttpRequestData request)
    {
        using StreamReader reader = new(request.Body);
        var bodyAsString = await reader.ReadToEndAsync().ConfigureAwait(false);

        var encoding = Encoding.UTF8;
        var byteArray = encoding.GetBytes(bodyAsString);
        return new MemoryStream(byteArray);
    }

    public static CancellationToken GetCancellationToken(this HttpRequestData request, CancellationToken hostCancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var cancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                hostCancellationToken,
                request.FunctionContext.CancellationToken);

        var cancellationToken = cancellationTokenSource.Token;
        return cancellationToken;
    }

    public static async Task<HttpResponseData> CreateMissingContentTypeResponseAsync(this HttpRequestData request, CancellationToken cancellationToken)
    {
        return await GetContentTypeErrorResponseAsync(
            request,
            "Could not get Content-Type from request header. The supported values are: application/xml, application/json, application/ebix",
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task<HttpResponseData> CreateInvalidContentTypeResponseAsync(this HttpRequestData request, CancellationToken cancellationToken)
    {
        return await GetContentTypeErrorResponseAsync(
            request,
            "Could not parse desired document format from Content-Type in request header. The supported values are: application/xml, application/json, application/ebix",
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<HttpResponseData> GetContentTypeErrorResponseAsync(
        HttpRequestData request,
        string message,
        CancellationToken cancellationToken)
    {
        var missingContentTypeResponse = request.CreateResponse(
            HttpStatusCode.UnsupportedMediaType);
        await missingContentTypeResponse.WriteStringAsync(
                message,
                cancellationToken)
            .ConfigureAwait(false);
        return missingContentTypeResponse;
    }
}
