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

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AcceptanceTest.Exceptions;
using AcceptanceTest.Factories;

namespace AcceptanceTest.Drivers;

internal sealed class EdiDriver : IDisposable
{
    private readonly string _azpToken;
    private readonly HttpClient _httpClient;

    public EdiDriver(string azpToken)
    {
        _azpToken = azpToken;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://func-api-edi-u-001.azurewebsites.net/");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<Stream> RequestAggregatedMeasureDataAsync(string actorNumber, string[] marketRoles, bool badRequest = false)
    {
        var token = TokenBuilder.BuildToken(actorNumber, marketRoles, _azpToken);
        var response = await RequestAggregatedMeasureDataAsync(token, badRequest).ConfigureAwait(false);

        var document = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        return document;
    }

    public async Task<Stream> PeekMessageAsync(string actorNumber, string[] marketRoles)
    {
        var token = TokenBuilder.BuildToken(actorNumber, marketRoles, _azpToken);
        var stopWatch = Stopwatch.StartNew();
        while (stopWatch.ElapsedMilliseconds < 60000)
        {
            var peekResponse = await PeekAsync(token)
                .ConfigureAwait(false);
            if (peekResponse.StatusCode == HttpStatusCode.OK)
            {
                var document = await peekResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await DequeueAsync(token, GetMessageId(peekResponse)).ConfigureAwait(false);
                return document;
            }

            if (peekResponse.StatusCode != HttpStatusCode.NoContent)
            {
                throw new UnexpectedPeekResponseException($"Unexpected Peek response: {peekResponse.StatusCode}");
            }

            await Task.Delay(500).ConfigureAwait(false);
        }

        throw new TimeoutException("Unable to retrieve peek result within time limit");
    }

    public async Task PeekAcceptedAggregationMessageAsync(string actorNumber, string[] roles)
    {
        var documentStream = await PeekMessageAsync(actorNumber, roles).ConfigureAwait(false);
        var jsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(documentStream).ConfigureAwait(false);

        Assert.True(
            jsonElement.TryGetProperty(
                "NotifyAggregatedMeasureData_MarketDocument",
                out var marketDocument),
            "Remember to clean Actor queue by dequeuing all existing messages.");
        Assert.Equal("E31", marketDocument
            .GetProperty("type")
            .GetProperty("value")
            .GetString());
    }

    public async Task PeekRejectedMessageAsync(string actorNumber, string[] roles)
    {
        var documentStream = await PeekMessageAsync(actorNumber, roles).ConfigureAwait(false);
        var jsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(documentStream).ConfigureAwait(false);

        Assert.True(
            jsonElement.TryGetProperty(
                "RejectRequestAggregatedMeasureData_MarketDocument",
                out var marketDocument),
            "Remember to clean Actor queue by dequeuing all existing messages.");
        Assert.Equal("ERR", marketDocument
            .GetProperty("type")
            .GetProperty("value")
            .GetString());
    }

    private static string GetMessageId(HttpResponseMessage peekResponse)
    {
        return peekResponse.Headers.GetValues("MessageId").First();
    }

    private static string GetContent(bool badRequest = false)
    {
        var jsonContent = badRequest
            ? File.ReadAllText("Messages/json/RequestAggregatedMeasureDataWithWrongProcessType.json")
            : File.ReadAllText("Messages/json/RequestAggregatedMeasureData.json");

        jsonContent = jsonContent.Replace("{MessageId}", Guid.NewGuid().ToString(), StringComparison.InvariantCulture);
        jsonContent = jsonContent.Replace("{TransactionId}", Guid.NewGuid().ToString(), StringComparison.InvariantCulture);

        return jsonContent;
    }

    private async Task<HttpResponseMessage> RequestAggregatedMeasureDataAsync(string token, bool badRequest = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/RequestAggregatedMeasureMessageReceiver");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Content = new StringContent(GetContent(badRequest), Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var aggregatedMeasureDataResponse = await _httpClient.SendAsync(request).ConfigureAwait(false);
        if (aggregatedMeasureDataResponse.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await aggregatedMeasureDataResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new BadAggregatedMeasureDataRequestException($"responseContent: {responseContent}");
        }

        aggregatedMeasureDataResponse.EnsureSuccessStatusCode();

        return aggregatedMeasureDataResponse;
    }

    private async Task<HttpResponseMessage> PeekAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/peek/aggregations");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var peekResponse = await _httpClient.SendAsync(request).ConfigureAwait(false);
        peekResponse.EnsureSuccessStatusCode();
        return peekResponse;
    }

    private async Task DequeueAsync(string token, string messageId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/dequeue/{messageId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        var dequeueResponse = await _httpClient.SendAsync(request).ConfigureAwait(false);
        dequeueResponse.EnsureSuccessStatusCode();
    }
}
