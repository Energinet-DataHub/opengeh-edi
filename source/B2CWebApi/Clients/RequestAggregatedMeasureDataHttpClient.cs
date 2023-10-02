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
using System.Net.Http.Headers;
using Energinet.DataHub.Edi.Requests;
using Google.Protobuf;

namespace Energinet.DataHub.EDI.B2CWebApi.Clients;

public class RequestAggregatedMeasureDataHttpClient
{
    private readonly HttpClient _httpClient;

    public RequestAggregatedMeasureDataHttpClient(IHttpClientFactory httpClientFactory, Uri baseUri)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = baseUri;
    }

    public async Task<string> RequestAsync(RequestAggregatedMeasureData requestAggregatedMeasureData, string token, CancellationToken cancellationToken)
    {
        if (requestAggregatedMeasureData == null) throw new ArgumentNullException(nameof(requestAggregatedMeasureData));

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/B2CRequestAggregatedMeasureMessageReceiver");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Content = new ByteArrayContent(requestAggregatedMeasureData.ToByteArray());

        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.BadRequest)
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return string.Empty;
    }
}
