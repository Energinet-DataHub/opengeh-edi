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

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]
public sealed class AzureAuthenticationDriver : IDisposable
{
    private readonly string _tenantId;
    private readonly string _backendAppId;
    private readonly ITestOutputHelper? _output;
    private readonly HttpClient _httpClient;

    public AzureAuthenticationDriver(string tenantId, string backendAppId, ITestOutputHelper? output = null)
    {
        _tenantId = tenantId;
        _backendAppId = backendAppId;
        _output = output;
        _httpClient = new HttpClient();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<string> GetB2BTokenAsync(string clientId, string clientSecret)
    {
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));
        if (clientSecret == null) throw new ArgumentNullException(nameof(clientSecret));
        if (_output != null)
        {
            var t = clientSecret.Substring(0, 4);
            var c = clientId.Substring(0, 4);
            _output.WriteLine("B2C tenant id: " + _tenantId);
            _output.WriteLine("AzureEntraBackendAppId: " + _backendAppId);
            _output.WriteLine("ClientId: " + c);
            _output.WriteLine("ClientSecret: " + t);
            _output.WriteLine("MeteredDataResponsibleCredential ClientSecret length: " + clientSecret.Length);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token", UriKind.Absolute));

        request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", clientId),
            new("client_secret", clientSecret),
            new("scope", $"{_backendAppId}/.default"),
        });

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var accessToken = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();

        if (string.IsNullOrEmpty(accessToken?.AccessToken))
            throw new JsonException($"Couldn't parse Azure AD access token. Response content: {await response.Content.ReadAsStringAsync()}");

        return accessToken.AccessToken;
    }
}

public sealed record AccessTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
