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
using Energinet.DataHub.EDI.AcceptanceTests.Drivers.B2C.Client;
using Energinet.DataHub.EDI.AcceptanceTests.Logging;
using Energinet.DataHub.EDI.AcceptanceTests.Responses.Json;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Xunit.Abstractions;
using SearchArchivedMessagesCriteria = Energinet.DataHub.EDI.B2CWebApi.Models.SearchArchivedMessagesCriteria;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers.B2C;

public sealed class B2CEdiDriver : IDisposable
{
    private readonly AsyncLazy<HttpClient> _httpClient;
    private readonly Uri _apiManagementUri;
    private readonly Uri _b2cWebApiUri;
    private readonly ITestOutputHelper _logger;

    public B2CEdiDriver(AsyncLazy<HttpClient> b2CHttpClient, Uri apiManagementUri, Uri b2cWebApiUri, ITestOutputHelper logger)
    {
        _httpClient = b2CHttpClient;
        _apiManagementUri = apiManagementUri;
        _b2cWebApiUri = b2cWebApiUri;
        _logger = logger;
    }

    public void Dispose()
    {
    }

    public async Task<List<ArchivedMessageSearchResponse>> SearchArchivedMessagesAsync(SearchArchivedMessagesCriteria parameters)
    {
        var b2cClient = await _httpClient;
        ArgumentNullException.ThrowIfNull(parameters);
        var parametersAsJson = JsonConvert.SerializeObject(parameters);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_apiManagementUri, "b2c/v1.0/ArchivedMessageSearch"));
        request.Content = new StringContent(parametersAsJson, Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await b2cClient.SendAsync(request).ConfigureAwait(false);
        await response.EnsureSuccessStatusCodeWithLogAsync(_logger);

        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        var archivedMessageResponse = JsonConvert.DeserializeObject<List<ArchivedMessageSearchResponse>>(responseString) ?? throw new InvalidOperationException("Did not receive valid response");

        return archivedMessageResponse;
    }

    public async Task RequestAggregatedMeasureDataAsync(CancellationToken cancellationToken)
    {
        var webApiClient = await CreateWebApiClientAsync();

        await webApiClient.RequestAggregatedMeasureDataAsync(
                body: new RequestAggregatedMeasureDataMarketRequest
                {
                    CalculationType = CalculationType.BalanceFixing,
                    StartDate = "2024-09-24T23:00:00",
                    GridArea = "804",
                },
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<B2CEdiClient> CreateWebApiClientAsync()
    {
        var httpClient = await _httpClient;

        return new B2CEdiClient(_b2cWebApiUri.AbsoluteUri, httpClient);
    }
}
