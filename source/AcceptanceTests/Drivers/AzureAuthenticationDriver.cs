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

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]
public sealed class AzureAuthenticationDriver : IDisposable
{
    private readonly string _tenantId;
    private readonly string _backendAppId;
    private readonly Uri _azureB2CTenantUri;
    private readonly string _backendBffScope;
    private readonly string _frontendAppId;
    private readonly Uri _marketParticipantUri;
    private readonly HttpClient _httpClient;

    public AzureAuthenticationDriver(string tenantId, string backendAppId, Uri azureB2CTenantUri, string backendBffScope, string frontendAppId, Uri? marketParticipantUrl)
    {
        _tenantId = tenantId;
        _backendAppId = backendAppId;
        _azureB2CTenantUri = azureB2CTenantUri;
        _backendBffScope = backendBffScope;
        _frontendAppId = frontendAppId;
        _marketParticipantUri = marketParticipantUrl ?? new Uri("https://app-webapi-markpart-u-001.azurewebsites.net");
        _httpClient = new HttpClient();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<string> GetB2BTokenAsync(string clientId, string clientSecret)
    {
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

    public async Task<string> GetB2CTokenAsync(string username, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _azureB2CTenantUri);

        request.Content = new MultipartFormDataContent
        {
#pragma warning disable CA2000
            { new StringContent(username), "username" },
            { new StringContent(password), "password" },
            { new StringContent("password"), "grant_type" },
            { new StringContent($"openid {_backendBffScope} offline_access"), "scope" },
            { new StringContent(_frontendAppId), "client_id" },
            { new StringContent("token id_token"), "response_type" },
#pragma warning restore CA2000
        };

        var response = await _httpClient.SendAsync(request);

        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        var tokenResult = await response.Content.ReadFromJsonAsync<AccessTokenResponse>()
                    ?? throw new InvalidOperationException($"Couldn't get acceptance test B2C token for user {username}");

        var tokenFromMarketParticipant = await GetB2CTokenWithPermissionsFromMarketParticipantAsync(tokenResult.AccessToken);

        return tokenFromMarketParticipant;
    }

    private async Task<string> GetB2CTokenWithPermissionsFromMarketParticipantAsync(string azureAdToken)
    {
        var actorId = await GetActorIdFromTokenAsync(azureAdToken);

        using var response = await _httpClient.PostAsJsonAsync(
            new Uri(_marketParticipantUri, $"token"),
            new { ActorId = actorId, ExternalToken = azureAdToken, });

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenFromMarketParticipantResponse>()
                    ?? throw new InvalidOperationException("Couldn't get acceptance test token from market participant");

        return token.Token;
    }

    private async Task<Guid> GetActorIdFromTokenAsync(string azureAdToken)
    {
        var actorResponse = await _httpClient.GetFromJsonAsync<ActorResponse>(new Uri(_marketParticipantUri, $"user/actors?externalToken={azureAdToken}"))
                            ?? throw new InvalidOperationException("Couldn't get acceptance test actor from azure ad token");

        var actorFromToken = actorResponse.ActorIds?.FirstOrDefault() ?? throw new InvalidOperationException("The user requested for the domain test does not have actors assigned");

        return actorFromToken;
    }

    private sealed record ActorResponse(IEnumerable<Guid>? ActorIds);

    private sealed record TokenFromMarketParticipantResponse(string Token);

    private sealed record AccessTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
}
