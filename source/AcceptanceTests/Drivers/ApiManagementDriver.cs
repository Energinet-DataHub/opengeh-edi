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
using System.Text.Json;
using Energinet.DataHub.EDI.AcceptanceTests.Exceptions;
using Energinet.DataHub.EDI.AcceptanceTests.Factories;
using Microsoft.Azure.Amqp.Framing;
using Newtonsoft.Json.Linq;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

#pragma warning disable VSTHRD200 // Since this is a DSL we don't want to suffix tasks with 'Async' since it is not part of the ubiquitous language

public sealed class ApiManagementDriver : IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://apim-shared-sharedres-u-001.azure-api.net/"),
    };

    private readonly string _tenantId;
    private readonly string _backendAppId;

    public ApiManagementDriver(string tenantId, string backendAppId)
    {
        _tenantId = tenantId;
        _backendAppId = backendAppId;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<string> GetAzureAdToken(string clientId, string clientSecret)
    {
        //var tenantId = "4a7411ea-ac71-4b63-9647-b8bd4c5a20e0";
        //var backendAppId = "fe8b720c-fda4-4aaa-9c6d-c0d2ed6584fe";
        //var clientId = "D8E67800-B7EF-4025-90BB-FE06E1639117";

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token", UriKind.Absolute));

        request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", clientId),
            new("client_secret", clientSecret),
            new("scope", $"{_backendAppId}/.default"),
        });

        var response = await _httpClient.SendAsync(request);

        var resultContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        var resultAsJObject = JObject.Parse(resultContent);

        var accessToken = resultAsJObject.Value<string>("access_token");

        if (string.IsNullOrEmpty(accessToken))
            throw new JsonException($"Couldn't parse Azure AD access token. Response content: {resultContent}");

        return accessToken;
    }

    public async Task<string> PeekEbixDocumentWithTimeoutAsync(string token)
    {
        var stopWatch = Stopwatch.StartNew();
        while (stopWatch.ElapsedMilliseconds < 60000)
        {
            var peekResponse = await PeekEbixAsync(token)
                .ConfigureAwait(false);
            if (peekResponse.StatusCode == HttpStatusCode.OK)
            {
                var document = await peekResponse.Content.ReadAsStringAsync();
                //await DequeueAsync(token, GetMessageId(peekResponse)).ConfigureAwait(false);
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

    private async Task<HttpResponseMessage> PeekEbixAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("ebix?soapAction=peekMessage", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        request.Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/ebix");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/ebix");

        var peekResponse = await _httpClient.SendAsync(request);

        peekResponse.EnsureSuccessStatusCode();

        return peekResponse;
    }
}
