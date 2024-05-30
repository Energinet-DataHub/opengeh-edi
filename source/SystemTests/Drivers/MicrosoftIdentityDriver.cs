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
using Energinet.DataHub.EDI.SystemTests.Logging;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.SystemTests.Drivers;

internal sealed class MicrosoftIdentityDriver
{
    private readonly TestLogger _logger;
    private readonly string _tenantId;
    private readonly string _backendAppId;

    internal MicrosoftIdentityDriver(TestLogger logger, string tenantId, string backendAppId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(backendAppId);
        _logger = logger;
        _tenantId = tenantId;
        _backendAppId = backendAppId;
    }

    internal async Task<string> GetB2BTokenAsync(string clientId, string clientSecret, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(clientId);
        ArgumentNullException.ThrowIfNull(clientSecret);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token", UriKind.Absolute));

        request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", clientId),
            new("client_secret", clientSecret),
            new("scope", $"{_backendAppId}/.default"),
        });

        using var httpClient = new HttpClient();
        var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await response.EnsureSuccessStatusCodeWithLogAsync(_logger);

        var accessToken = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(accessToken?.AccessToken))
            throw new JsonException($"Couldn't parse Azure AD access token. Response content: {await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)}");

        return accessToken.AccessToken;
    }
}

internal sealed record AccessTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
