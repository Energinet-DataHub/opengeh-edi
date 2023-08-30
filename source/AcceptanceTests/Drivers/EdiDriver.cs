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
using AcceptanceTest.Exceptions;
using AcceptanceTest.Factories;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf;

namespace AcceptanceTest.Drivers;

internal sealed class EdiDriver : IDisposable
{
    private readonly string _azpToken;
    private readonly HttpClient _httpClient;
    private readonly EdiInboxPublisher _ediInboxPublisher;

    public EdiDriver(string azpToken, EdiInboxPublisher ediInboxPublisher)
    {
        _azpToken = azpToken;
        _ediInboxPublisher = ediInboxPublisher;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://func-api-edi-u-001.azurewebsites.net/");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<Stream> RequestAggregatedMeasureDataAsync(string actorNumber, string[] marketRoles)
    {
        var token = TokenBuilder.BuildToken(actorNumber, marketRoles, _azpToken);
        var response = await RequestAggregatedMeasureDataAsync(token).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            //Throw exception
        }

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

    internal Task SendAggregatedMeasureDataAsync()
    {
        return _ediInboxPublisher.SendToInboxAsync(
            "AggregatedMeasureDataAccepted",
            CreateAggregationMeasureDataAccepted().ToByteArray());
    }

    private static string GetMessageId(HttpResponseMessage peekResponse)
    {
        return peekResponse.Headers.GetValues("MessageId").First();
    }

    private static IMessage CreateAggregationMeasureDataAccepted()
    {
        return new AggregatedTimeSeriesRequestAccepted();
    }

    private static string GetContent()
    {
        //var jsonFilePath = "RequestAggregatedMeasureData.json";
        //var jsonContent = File.ReadAllText(jsonFilePath);
        var jsonContent = File.ReadAllText($"Drivers/RequestAggregatedMeasureData.json");
        return jsonContent;
    }

    private async Task<HttpResponseMessage> RequestAggregatedMeasureDataAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/RequestAggregatedMeasureMessageReceiver");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Content = new StringContent(GetContent(), Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var aggregatedMeasureDataResponse = await _httpClient.SendAsync(request).ConfigureAwait(false);
        aggregatedMeasureDataResponse.EnsureSuccessStatusCode();
        return aggregatedMeasureDataResponse;
    }

    private async Task<HttpResponseMessage> PeekAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/peek/aggregations");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/xml");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
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
