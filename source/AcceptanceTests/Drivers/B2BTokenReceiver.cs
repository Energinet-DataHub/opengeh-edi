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

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

public class B2BTokenReceiver
{
    private readonly HttpClient _httpClient;
    private readonly string _tenantId;
    private readonly string _backendAppId;

    public B2BTokenReceiver(HttpClient httpClient, string tenantId, string backendAppId)
    {
        _tenantId = tenantId;
        _backendAppId = backendAppId;
        _httpClient = httpClient;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<string> GetB2BTokenAsync(string clientId, string clientSecret)
    {
        ArgumentNullException.ThrowIfNull(clientId);
        ArgumentNullException.ThrowIfNull(clientSecret);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token", UriKind.Absolute));

        request.Content = new FormUrlEncodedContent([
            new("grant_type", "client_credentials"),
            new("client_id", clientId),
            new("client_secret", clientSecret),
            new("scope", $"{_backendAppId}/.default"),
        ]);

        var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var accessToken = await response.Content.ReadFromJsonAsync<AccessTokenResponse>().ConfigureAwait(false);

        if (string.IsNullOrEmpty(accessToken?.AccessToken))
            throw new JsonException($"Couldn't parse Azure AD access token. Response content: {await response.Content.ReadAsStringAsync().ConfigureAwait(false)}");

        return accessToken.AccessToken;
    }
}
