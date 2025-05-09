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

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Energinet.DataHub.EDI.B2BApi.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;
using Energinet.DataHub.ProcessManager.Components.Abstractions.EnqueueActorMessages;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.Shared.V1.Model;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Extensions;

public static class HttpRequestExtensions
{
    public static Task<HttpRequestMessage> CreateRequestWholesaleServicesHttpRequestAsync(
        this B2BApiAppFixture fixture,
        Actor actor,
        string? transactionId = null)
    {
        var documentPath = actor.ActorRole.Name switch
        {
            _ when actor.ActorRole.Name == ActorRole.EnergySupplier.Name => "TestData/Messages/xml/RequestWholesaleSettlementForEnergySupplier.xml",
            _ => throw new ArgumentOutOfRangeException(actor.ActorRole.Name),
        };

        return CreateIncomingMessageHttpRequestAsync(
            fixture,
            documentPath,
            IncomingDocumentType.RequestWholesaleSettlement.Name,
            "application/xml",
            actor,
            transactionId);
    }

    public static Task<HttpRequestMessage> CreateRequestAggregatedMeasureDataHttpRequestAsync(
        this B2BApiAppFixture fixture,
        Actor actor,
        string? transactionId = null)
    {
        var documentPath = actor.ActorRole.Name switch
        {
            _ when actor.ActorRole.Name == ActorRole.EnergySupplier.Name => "TestData/Messages/json/RequestAggregatedMeasureDataForEnergySupplier.json",
            _ => throw new ArgumentOutOfRangeException(actor.ActorRole.Name),
        };

        return CreateIncomingMessageHttpRequestAsync(
            fixture,
            documentPath,
            IncomingDocumentType.RequestAggregatedMeasureData.Name,
            "application/json",
            actor,
            transactionId);
    }

    public static Task<HttpRequestMessage> CreatePeekHttpRequestAsync(
        this B2BApiAppFixture fixture,
        Actor actor,
        MessageCategory category)
    {
        return CreateHttpRequestAsync(
            fixture,
            actor,
            HttpMethod.Get,
            $"api/peek/{category.Name}",
            new StringContent(string.Empty, new MediaTypeHeaderValue("application/json")));
    }

    public static Task<HttpRequestMessage> CreateDequeueHttpRequestAsync(
        this B2BApiAppFixture fixture,
        Actor actor,
        string messageId)
    {
        return CreateHttpRequestAsync(
            fixture,
            actor,
            HttpMethod.Delete,
            $"api/dequeue/{messageId}");
    }

    public static HttpRequestMessage CreateEnqueueMessagesHttpRequest(
        this B2BApiAppFixture fixture,
        IEnqueueDataSyncDto enqueueActorMessagesHttp)
    {
        HttpRequestMessage? request = null;
        try
        {
            request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/enqueue/{enqueueActorMessagesHttp.Route}")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(enqueueActorMessagesHttp, enqueueActorMessagesHttp.GetType()),
                    Encoding.UTF8,
                    "application/json"),
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", fixture.CreateSubsystemToken());

            return request;
        }
        catch
        {
            request?.Dispose();
            throw;
        }
    }

    private static async Task<HttpRequestMessage> CreateIncomingMessageHttpRequestAsync(
        B2BApiAppFixture fixture,
        string filePath,
        string documentType,
        string contentType,
        Actor actor,
        string? transactionId)
    {
        HttpRequestMessage? request = null;
        try
        {
            var document = await File.ReadAllTextAsync(filePath);
            document = document
                .Replace("{ActorNumber}", actor.ActorNumber.Value)
                .Replace("{ActorRole}", actor.ActorRole.Code)
                .Replace("{MessageId}", Guid.NewGuid().ToString())
                .Replace("{TransactionId}", transactionId ?? Guid.NewGuid().ToString());

            request = await CreateHttpRequestAsync(
                fixture,
                actor,
                HttpMethod.Post,
                $"api/incomingMessages/{documentType}",
                new StringContent(
                    document,
                    Encoding.UTF8,
                    contentType));

            return request;
        }
        catch
        {
            request?.Dispose();
            throw;
        }
    }

    private static async Task<HttpRequestMessage> CreateHttpRequestAsync(
        B2BApiAppFixture fixture,
        Actor actor,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // The actor must exist in the database. It doesn't matter if the actor number exists more than once in the
        // database, as long as an actor with the client id from the b2b token exists.
        var actorClientId = Guid.NewGuid().ToString();
        await fixture.DatabaseManager.AddActorAsync(actor.ActorNumber, actorClientId);

        // The bearer token must contain:
        //  * the actor role matching the document content
        //  * the external id matching the actor in the database
        var b2bToken = new JwtBuilder()
            .WithRole(ClaimsMap.RoleFrom(actor.ActorRole).Value)
            .WithClaim(ClaimsMap.ActorClientId, actorClientId)
            .CreateToken();

        var request = new HttpRequestMessage(method, url)
        {
            Content = content,
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", b2bToken);
        return request;
    }
}
