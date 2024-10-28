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

using System.Net.Http.Headers;
using System.Text;
using Energinet.DataHub.EDI.B2BApi.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Extensions;

public static class HttpRequestExtensions
{
    public static Task<HttpRequestMessage> CreateRequestWholesaleServicesHttpRequestAsync(
        this B2BApiAppFixture fixture,
        Actor actor)
    {
        return CreateHttpRequestAsync(
            fixture,
            "TestData/Messages/xml/RequestWholesaleSettlement.xml",
            IncomingDocumentType.RequestWholesaleSettlement.Name,
            "application/xml",
            actor);
    }

    private static async Task<HttpRequestMessage> CreateHttpRequestAsync(
        B2BApiAppFixture fixture,
        string filePath,
        string documentType,
        string contentType,
        Actor actor)
    {
        HttpRequestMessage? request = null;
        try
        {
            var document = await File.ReadAllTextAsync(filePath);
            document = document
                .Replace("{MessageId}", Guid.NewGuid().ToString())
                .Replace("{TransactionId}", Guid.NewGuid().ToString());

            // The actor must exist in the database
            var externalId = Guid.NewGuid().ToString();
            await fixture.DatabaseManager.AddActorAsync(actor.ActorNumber, externalId);

            // The bearer token must contain:
            //  * the actor role matching the document content
            //  * the external id matching the actor in the database
            var b2bToken = new JwtBuilder()
                .WithRole(ClaimsMap.RoleFrom(actor.ActorRole).Value)
                .WithClaim(ClaimsMap.ActorId, externalId)
                .CreateToken();

            request = new HttpRequestMessage(HttpMethod.Post, $"api/incomingMessages/{documentType}")
            {
                Content = new StringContent(
                    document,
                    Encoding.UTF8,
                    contentType),
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", b2bToken);

            return request;
        }
        catch
        {
            request?.Dispose();
            throw;
        }
    }
}
