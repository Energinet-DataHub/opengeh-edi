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

using System.Net.Http.Headers;
using System.Text;
using Energinet.DataHub.EDI.AcceptanceTests.Responses.json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

public sealed class EdiB2CDriver : IDisposable
{
    private readonly AsyncLazy<HttpClient> _httpClient;
    private readonly Uri _apiManagementUri;

    public EdiB2CDriver(AsyncLazy<HttpClient> b2CHttpClient, Uri apiManagementUri)
    {
        _httpClient = b2CHttpClient;
        _apiManagementUri = apiManagementUri;
    }

    public void Dispose()
    {
    }

    public async Task<List<ArchivedMessageSearchResponse>> RequestArchivedMessageSearchAsync(JObject payload)
    {
        var b2cClient = await _httpClient;
        ArgumentNullException.ThrowIfNull(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_apiManagementUri, "b2c/v1.0/ArchivedMessageSearch"));
        request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await b2cClient.SendAsync(request).ConfigureAwait(false);
        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        var archivedMessageResponse = JsonConvert.DeserializeObject<List<ArchivedMessageSearchResponse>>(responseString) ?? throw new InvalidOperationException("Did not receive valid response");

        return archivedMessageResponse;
    }
}
