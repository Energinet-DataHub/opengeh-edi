﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Xml;
using Energinet.DataHub.EDI.AcceptanceTests.Exceptions;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using Nito.AsyncEx;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

internal sealed class EdiDriver : IDisposable
{
    private readonly AsyncLazy<HttpClient> _httpClient;

    public EdiDriver(AsyncLazy<HttpClient> b2bHttpClient)
    {
        _httpClient = b2bHttpClient;
    }

    public void Dispose()
    {
    }

    public async Task<Stream> RequestAggregatedMeasureDataAsync(bool asyncError = false, string? token = null)
    {
        var b2bClient = await _httpClient;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1.0/cim/requestaggregatedmeasuredata");
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Content = new StringContent(GetContent(asyncError), Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var aggregatedMeasureDataResponse = await b2bClient.SendAsync(request).ConfigureAwait(false);
        if (aggregatedMeasureDataResponse.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await aggregatedMeasureDataResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new BadAggregatedMeasureDataRequestException($"responseContent: {responseContent}");
        }

        aggregatedMeasureDataResponse.EnsureSuccessStatusCode();

        var document = await aggregatedMeasureDataResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
        return document;
    }

    public async Task<Stream> RequestWholesaleSettlementAsync(DocumentFormat documentFormat)
    {
        var b2bClient = await _httpClient;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1.0/cim/requestwholesalesettlement");
        var contentType = DocumentFormat.Json == documentFormat ? "application/json" : "application/xml";
        request.Content = new StringContent(GetRequestWholesaleSettlementContent(documentFormat), Encoding.UTF8, contentType);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var wholesaleSettlementResponse = await b2bClient.SendAsync(request).ConfigureAwait(false);
        if (wholesaleSettlementResponse.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await wholesaleSettlementResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new BadAggregatedMeasureDataRequestException($"responseContent: {responseContent}");
        }

        wholesaleSettlementResponse.EnsureSuccessStatusCode();

        var document = await wholesaleSettlementResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
        return document;
    }

    public async Task<Stream> PeekMessageAsync(DocumentFormat? documentFormat = null)
    {
        var stopWatch = Stopwatch.StartNew();
        while (stopWatch.ElapsedMilliseconds < 600000)
        {
            var peekResponse = await PeekAsync(documentFormat)
                .ConfigureAwait(false);
            if (peekResponse.StatusCode == HttpStatusCode.OK)
            {
                var document = await peekResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await DequeueAsync(GetMessageId(peekResponse)).ConfigureAwait(false);
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

    public async Task EmptyQueueAsync()
    {
        var peekResponse = await PeekAsync()
                .ConfigureAwait(false);
        if (peekResponse.StatusCode == HttpStatusCode.OK)
        {
            await DequeueAsync(GetMessageId(peekResponse)).ConfigureAwait(false);
            await EmptyQueueAsync().ConfigureAwait(false);
        }
    }

    public async Task PeekAcceptedAggregationMessageAsync()
    {
        var documentStream = await PeekMessageAsync().ConfigureAwait(false);
        var jsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(documentStream).ConfigureAwait(false);

        var documentIsOfExpectedType = jsonElement.TryGetProperty(
            "NotifyAggregatedMeasureData_MarketDocument",
            out var marketDocument);

        Assert.True(documentIsOfExpectedType, "\nAccepted message failed with wrong message type\n Document: " + jsonElement.ToString() + "\n");
        Assert.Equal("E31", marketDocument
            .GetProperty("type")
            .GetProperty("value")
            .GetString());
    }

    public async Task PeekWholesaleSettlementResponseAsync(DocumentFormat documentFormat, string expectedDocumentType)
    {
        var documentStream = await PeekMessageAsync(documentFormat).ConfigureAwait(false);

        if (documentFormat == DocumentFormat.Json)
        {
            var jsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(documentStream).ConfigureAwait(false);

            var documentIsOfExpectedType = jsonElement.TryGetProperty(
                expectedDocumentType,
                out var marketDocument);

            Assert.True(documentIsOfExpectedType, "\nAccepted message failed with wrong message type\n Document: " + jsonElement.ToString() + "\n");
            return;
        }

        using var reader = new StreamReader(documentStream);
        string text = await reader.ReadToEndAsync().ConfigureAwait(false);
        text.Should().Contain(expectedDocumentType, "\nAccepted message failed with wrong message type\n Document: " + text + "\n");
    }

    public async Task PeekRejectedMessageAsync()
    {
        var documentStream = await PeekMessageAsync().ConfigureAwait(false);
        var jsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(documentStream).ConfigureAwait(false);

        var documentIsOfExpectedType = jsonElement.TryGetProperty(
            "RejectRequestAggregatedMeasureData_MarketDocument",
            out var marketDocument);

        Assert.True(documentIsOfExpectedType, "\nRejected message failed with wrong message type\n");
        Assert.Equal("ERR", marketDocument
            .GetProperty("type")
            .GetProperty("value")
            .GetString());
    }

    public async Task RequestAggregatedMeasureDataWithoutTokenAsync()
    {
        var act = () => RequestAggregatedMeasureDataAsync(false, string.Empty);

        var httpRequestException = await Assert.ThrowsAsync<HttpRequestException>(act).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Unauthorized, httpRequestException.StatusCode);
    }

    public async Task PeekMessageWithoutTokenAsync()
    {
        var act = async () =>
        {
            var b2bClient = await _httpClient;
            using var request = new HttpRequestMessage(HttpMethod.Get, "v1.0/cim/aggregations");
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", null);
            request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var peekResponse = await b2bClient.SendAsync(request).ConfigureAwait(false);
            peekResponse.EnsureSuccessStatusCode();
        };

        var httpRequestException = await Assert.ThrowsAsync<HttpRequestException>(act).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Unauthorized, httpRequestException.StatusCode);
    }

    public async Task DequeueMessageWithoutTokenAsync(string messageId)
    {
        var act = async () =>
        {
            var b2bClient = await _httpClient;
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"v1.0/cim/dequeue/{messageId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", null);
            var dequeueResponse = await b2bClient.SendAsync(request).ConfigureAwait(false);
            dequeueResponse.EnsureSuccessStatusCode();
        };

        var httpRequestException = await Assert.ThrowsAsync<HttpRequestException>(act).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Unauthorized, httpRequestException.StatusCode);
    }

    public async Task<string> RequestAggregatedMeasureDataXmlAsync(XmlDocument payload, string? token = null)
    {
        var b2bClient = await _httpClient;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1.0/cim/requestaggregatedmeasuredata");
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Content = new StringContent(payload.OuterXml, Encoding.UTF8, "application/xml");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
        var response = await b2bClient.SendAsync(request).ConfigureAwait(false);
        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        return responseString;
    }

    private static string GetMessageId(HttpResponseMessage peekResponse)
    {
        return peekResponse.Headers.GetValues("MessageId").First();
    }

    private static string GetContent(bool forceAsyncError, ActorRole? marketRole = null)
    {
        string jsonRequestAcceptedFilePath;
        string jsonRequestAsynchronousRejectedFilePath;

        switch (marketRole?.Code)
        {
            case WholesaleDriver.BalanceResponsiblePartyMarketRoleCode:
                jsonRequestAcceptedFilePath = "Messages/json/RequestAggregatedMeasureDataBalanceResponsible.json";
                jsonRequestAsynchronousRejectedFilePath = "Messages/json/RequestAggregatedMeasureDataBalanceResponsibleWithBadPeriod.json";
                break;

            default:
                jsonRequestAcceptedFilePath = "Messages/json/RequestAggregatedMeasureData.json";
                jsonRequestAsynchronousRejectedFilePath = "Messages/json/RequestAggregatedMeasureDataWithBadPeriod.json";
                break;
        }

        var jsonFilePath = forceAsyncError
            ? jsonRequestAsynchronousRejectedFilePath
            : jsonRequestAcceptedFilePath;

        var jsonContent = File.ReadAllText(jsonFilePath);

        jsonContent = jsonContent.Replace("{MessageId}", Guid.NewGuid().ToString(), StringComparison.InvariantCulture);
        jsonContent = jsonContent.Replace("{TransactionId}", Guid.NewGuid().ToString(), StringComparison.InvariantCulture);

        return jsonContent;
    }

    private static string GetRequestWholesaleSettlementContent(DocumentFormat documentFormat)
    {
        if (DocumentFormat.Json == documentFormat)
        {
            var jsonContent = File.ReadAllText("Messages/json/RequestWholesaleSettlement.json");

            jsonContent = jsonContent.Replace("{MessageId}", Guid.NewGuid().ToString(), StringComparison.InvariantCulture);
            jsonContent = jsonContent.Replace("{TransactionId}", Guid.NewGuid().ToString(), StringComparison.InvariantCulture);

            return jsonContent;
        }

        var xmlContent = File.ReadAllText("Messages/xml/RequestWholesaleSettlementWithBadPeriod.xml");

        xmlContent = xmlContent.Replace("{MessageId}", Guid.NewGuid().ToString(), StringComparison.InvariantCulture);
        xmlContent = xmlContent.Replace("{TransactionId}", Guid.NewGuid().ToString(), StringComparison.InvariantCulture);

        return xmlContent;
    }

    private async Task<HttpResponseMessage> PeekAsync(DocumentFormat? documentFormat = null)
    {
        var b2bClient = await _httpClient;
        using var request = new HttpRequestMessage(HttpMethod.Get, "v1.0/cim/aggregations");
        var contentType = documentFormat == null || DocumentFormat.Json == documentFormat ? "application/json" : "application/xml";
        request.Content = new StringContent(string.Empty, Encoding.UTF8, contentType);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var peekResponse = await b2bClient.SendAsync(request).ConfigureAwait(false);
        peekResponse.EnsureSuccessStatusCode();
        return peekResponse;
    }

    private async Task DequeueAsync(string messageId)
    {
        var b2bClient = await _httpClient;
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"v1.0/cim/dequeue/{messageId}");
        var dequeueResponse = await b2bClient.SendAsync(request).ConfigureAwait(false);
        dequeueResponse.EnsureSuccessStatusCode();
    }
}
