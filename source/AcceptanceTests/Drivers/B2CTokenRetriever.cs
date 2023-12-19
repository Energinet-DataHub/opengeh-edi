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

using System.Net.Http.Json;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

public class B2CTokenRetriever
{
    private readonly HttpClient _httpClient;
    private readonly Uri _azureB2CTenantUri;
    private readonly string _backendBffScope;
    private readonly string _frontendAppId;
    private readonly Uri _marketParticipantUri;

    public B2CTokenRetriever(HttpClient httpClient, Uri azureB2CTenantUri, string backendBffScope, string frontendAppId, Uri marketParticipantUri)
    {
        _httpClient = httpClient;
        _azureB2CTenantUri = azureB2CTenantUri;
        _backendBffScope = backendBffScope;
        _frontendAppId = frontendAppId;
        _marketParticipantUri = marketParticipantUri;
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

        var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var tokenResult = await response.Content.ReadFromJsonAsync<AccessTokenResponse>().ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"Couldn't get acceptance test B2C token for user {username}");

        var tokenFromMarketParticipant = await GetB2CTokenWithPermissionsFromMarketParticipantAsync(tokenResult.AccessToken).ConfigureAwait(false);

        return tokenFromMarketParticipant;
    }

    private async Task<string> GetB2CTokenWithPermissionsFromMarketParticipantAsync(string azureAdToken)
    {
        var actorId = await GetActorIdFromTokenAsync(azureAdToken).ConfigureAwait(false);

        using var response = await _httpClient.PostAsJsonAsync(
            new Uri(_marketParticipantUri, $"token"),
            new { ActorId = actorId, ExternalToken = azureAdToken, }).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenFromMarketParticipantResponse>().ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Couldn't get acceptance test token from market participant");

        return token.Token;
    }

    private async Task<Guid> GetActorIdFromTokenAsync(string azureAdToken)
    {
        var actorResponse = await _httpClient.GetFromJsonAsync<ActorResponse>(new Uri(_marketParticipantUri, $"user/actors?externalToken={azureAdToken}")).ConfigureAwait(false)
                            ?? throw new InvalidOperationException("Couldn't get acceptance test actor from azure ad token");

        var actorFromToken = actorResponse.ActorIds?.FirstOrDefault() ?? throw new InvalidOperationException("The user requested for the domain test does not have actors assigned");

        return actorFromToken;
    }

    private sealed record ActorResponse(IEnumerable<Guid>? ActorIds);

    private sealed record TokenFromMarketParticipantResponse(string Token);
}
