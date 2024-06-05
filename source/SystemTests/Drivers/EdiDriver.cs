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
using Energinet.DataHub.EDI.SystemTests.Exceptions;
using Energinet.DataHub.EDI.SystemTests.Logging;
using Energinet.DataHub.EDI.SystemTests.Models;
using Nito.AsyncEx;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.SystemTests.Drivers;

public sealed class EdiDriver
{
    private readonly TestLogger _logger;
    private readonly AsyncLazy<HttpClient> _httpClient;
    private readonly MicrosoftIdentityDriver _microsoftIdentityDriver;

    internal EdiDriver(TestLogger logger, Uri apiManagementUri, string tenantId, string backendAppId)
    {
        _logger = logger;
        ArgumentNullException.ThrowIfNull(apiManagementUri);
        _httpClient = new AsyncLazy<HttpClient>(() => GetHttpClientAsync(apiManagementUri));
        _microsoftIdentityDriver = new MicrosoftIdentityDriver(logger, tenantId, backendAppId);
    }

    internal async Task<HttpResponseMessage> PeekAsync(Actor? actor, CancellationToken cancellationToken)
    {
        var httpClient = await _httpClient;
        await AddAuthTokenToRequestAsync(actor, httpClient, cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(HttpMethod.Get, "v1.0/cim/aggregations");
        var contentType = "application/json";
        request.Content = new StringContent(string.Empty, Encoding.UTF8, contentType);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var peekResponse = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureResponseSuccessAsync(peekResponse);
        return peekResponse;
    }

    internal async Task DequeueAsync(Actor? actor, string messageId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messageId);

        var httpClient = await _httpClient;
        await AddAuthTokenToRequestAsync(actor, httpClient, cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"v1.0/cim/dequeue/{messageId}");
        var dequeueResponse = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureResponseSuccessAsync(dequeueResponse);
    }

    internal async Task SendRequestAsync(Actor? actor, MessageType messageType, CancellationToken cancellationToken)
    {
        var httpClient = await _httpClient;
        await AddAuthTokenToRequestAsync(actor, httpClient, cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(HttpMethod.Post, GetRequestPath(messageType));
        request.Content = new StringContent(GetContent(messageType), Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureResponseSuccessAsync(response);
    }

    /// <summary>
    ///  Calls Peek until a response is received or the timeout is reached.
    /// </summary>
    internal async Task<HttpResponseMessage> PeekUntilResponseAsync(
        Actor actor,
        CancellationToken cancellationToken,
        int timeoutInMs = 600000)
    {
        var stopWatch = Stopwatch.StartNew();
        while (stopWatch.ElapsedMilliseconds < timeoutInMs)
        {
            var peekResponse = await PeekAsync(actor, cancellationToken).ConfigureAwait(false);
            if (peekResponse.StatusCode == HttpStatusCode.OK)
            {
                return peekResponse;
            }

            if (peekResponse.StatusCode != HttpStatusCode.NoContent)
            {
                throw new UnexpectedPeekResponseException($"Unexpected Peek response: {peekResponse.StatusCode}");
            }

            await Task.Delay(500).ConfigureAwait(false);
        }

        throw new TimeoutException("Unable to retrieve peek result within time limit");
    }

    private static Task<HttpClient> GetHttpClientAsync(Uri apiManagementUri)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = apiManagementUri,
            Timeout = TimeSpan.FromMinutes(5),
        };

        return Task.FromResult(httpClient);
    }

    private static string GetRequestPath(MessageType messageType)
    {
        return messageType switch
        {
            MessageType.RequestAggregatedMeasureData or MessageType.InvalidRequestAggregatedMeasureData =>
                "v1.0/cim/requestaggregatedmeasuredata",
            MessageType.RequestWholesaleSettlement or MessageType.InvalidRequestWholesaleSettlement =>
                "v1.0/cim/requestwholesalesettlement",
            _ => throw new NotImplementedException(),
        };
    }

    private static string GetContent(MessageType messageType)
    {
        var filePath = messageType switch
        {
            MessageType.RequestAggregatedMeasureData =>
                "Messages/Json/RequestAggregatedMeasureData.json",
            MessageType.InvalidRequestAggregatedMeasureData =>
                "Messages/Json/RequestAggregatedMeasureDataWithBadPeriod.json",
            MessageType.RequestWholesaleSettlement =>
                "Messages/Json/RequestWholesaleSettlement.json",
            MessageType.InvalidRequestWholesaleSettlement =>
                "Messages/Json/RequestWholesaleSettlementWithBadPeriod.json",
            _ => throw new NotImplementedException(),
        };

        var jsonContent = File.ReadAllText(filePath);

        // MessageId and TransactionId most be unique for each message
        jsonContent = jsonContent.Replace("{MessageId}", Guid.NewGuid().ToString(), StringComparison.InvariantCulture);
        jsonContent = jsonContent.Replace("{TransactionId}", Guid.NewGuid().ToString(), StringComparison.InvariantCulture);

        return jsonContent;
    }

    private async Task<string?> GetTokenAsync(Actor actor, CancellationToken cancellationToken)
    {
        return await _microsoftIdentityDriver
            .GetB2BTokenAsync(actor.ClientId, actor.ClientSecret, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task AddAuthTokenToRequestAsync(Actor? actor, HttpClient httpClient, CancellationToken cancellationToken)
    {
        if (actor is not null)
        {
            var token = await GetTokenAsync(actor, cancellationToken).ConfigureAwait(false);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
        }
        else
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", null);
        }
    }

    private Task EnsureResponseSuccessAsync(HttpResponseMessage response) => response.EnsureSuccessStatusCodeWithLogAsync(_logger);
}
