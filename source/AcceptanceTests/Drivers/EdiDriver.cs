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
using System.Xml;
using Energinet.DataHub.EDI.AcceptanceTests.Exceptions;
using Energinet.DataHub.EDI.AcceptanceTests.TestData;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Microsoft.Data.SqlClient;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

internal sealed class EdiDriver : IDisposable
{
    private readonly string _connectionString;
    private readonly AzureAuthenticationDriver _authenticationDriver;
    private readonly HttpClient _httpClient;

    public EdiDriver(
        string connectionString,
        Uri ediB2BBaseUri,
        AzureAuthenticationDriver authenticationDriver)
    {
        _connectionString = connectionString;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = ediB2BBaseUri;
        _authenticationDriver = authenticationDriver;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<Stream> RequestAggregatedMeasureDataAsync(ActorCredential actorCredential, bool asyncError = false)
    {
        var token = await _authenticationDriver
            .GetB2BTokenAsync(actorCredential.ClientId, actorCredential.ClientSecret)
            .ConfigureAwait(false);
        var response = await RequestAggregatedMeasureDataAsync(token, asyncError).ConfigureAwait(false);

        var document = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        return document;
    }

    public async Task<Stream> PeekMessageAsync(ActorCredential actorCredential)
    {
        var token = await _authenticationDriver
            .GetB2BTokenAsync(actorCredential.ClientId, actorCredential.ClientSecret)
            .ConfigureAwait(false);

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

    public async Task EmptyQueueAsync(ActorCredential actorCredential)
    {
        var token = await _authenticationDriver
            .GetB2BTokenAsync(actorCredential.ClientId, actorCredential.ClientSecret)
            .ConfigureAwait(false);

        var peekResponse = await PeekAsync(token)
                .ConfigureAwait(false);
        if (peekResponse.StatusCode == HttpStatusCode.OK)
        {
            await DequeueAsync(token, GetMessageId(peekResponse)).ConfigureAwait(false);
            await EmptyQueueAsync(actorCredential).ConfigureAwait(false);
        }
    }

    public async Task PeekAcceptedAggregationMessageAsync(ActorCredential actorCredential)
    {
        var documentStream = await PeekMessageAsync(actorCredential).ConfigureAwait(false);
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

    public async Task PeekRejectedMessageAsync(ActorCredential actorCredential)
    {
        var documentStream = await PeekMessageAsync(actorCredential).ConfigureAwait(false);
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

    public async Task ActorExistsAsync(string actorNumber, string azpToken)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand();

        command.CommandText = "SELECT COUNT(*) FROM [Actor] WHERE ActorNumber = @ActorNumber AND ExternalId = @ExternalId";
        command.Parameters.AddWithValue("@ActorNumber", actorNumber);
        command.Parameters.AddWithValue("@ExternalId", azpToken);
        command.Connection = connection;

        await command.Connection.OpenAsync().ConfigureAwait(false);
        var exist = await command.ExecuteScalarAsync().ConfigureAwait(false);
        Assert.NotNull(exist);
    }

    public async Task RequestAggregatedMeasureDataWithoutTokenAsync()
    {
        var act = () => RequestAggregatedMeasureDataAsync(string.Empty, false);

        var httpRequestException = await Assert.ThrowsAsync<HttpRequestException>(act).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Unauthorized, httpRequestException.StatusCode);
    }

    public async Task PeekMessageWithoutTokenAsync()
    {
        var act = () => PeekAsync(null);

        var httpRequestException = await Assert.ThrowsAsync<HttpRequestException>(act).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Unauthorized, httpRequestException.StatusCode);
    }

    public async Task DequeueMessageWithoutTokenAsync(string messageId)
    {
        var act = () => DequeueAsync(null, messageId);

        var httpRequestException = await Assert.ThrowsAsync<HttpRequestException>(act).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Unauthorized, httpRequestException.StatusCode);
    }

    public async Task<string> RequestAggregatedMeasureDataXmlAsync(XmlDocument payload, ActorCredential actorCredential)
    {
        var token = await _authenticationDriver
            .GetB2BTokenAsync(actorCredential.ClientId, actorCredential.ClientSecret)
            .ConfigureAwait(false);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/RequestAggregatedMeasureMessageReceiver");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Content = new StringContent(payload.OuterXml, Encoding.UTF8, "application/xml");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
        var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return responseString;
    }

    private static string GetMessageId(HttpResponseMessage peekResponse)
    {
        return peekResponse.Headers.GetValues("MessageId").First();
    }

    private static string GetContent(bool forceAsyncError, MarketRole? marketRole = null)
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

    private async Task<HttpResponseMessage> RequestAggregatedMeasureDataAsync(string? token, bool asyncError)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/RequestAggregatedMeasureMessageReceiver");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Content = new StringContent(GetContent(asyncError), Encoding.UTF8, "application/json");
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

    private async Task<HttpResponseMessage> PeekAsync(string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/peek/aggregations");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var peekResponse = await _httpClient.SendAsync(request).ConfigureAwait(false);
        peekResponse.EnsureSuccessStatusCode();
        return peekResponse;
    }

    private async Task DequeueAsync(string? token, string messageId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/dequeue/{messageId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        var dequeueResponse = await _httpClient.SendAsync(request).ConfigureAwait(false);
        dequeueResponse.EnsureSuccessStatusCode();
    }
}
