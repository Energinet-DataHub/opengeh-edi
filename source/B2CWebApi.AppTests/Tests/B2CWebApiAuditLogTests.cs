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

using System.Net;
using System.Text.Json;
using Energinet.DataHub.EDI.AuditLog.AuditLogClient;
using Energinet.DataHub.EDI.B2CWebApi.AppTests.Fixture;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2CWebApi.AppTests.Tests;

[Collection(nameof(B2CWebApiCollectionFixture))]
public class B2CWebApiAuditLogTests : IAsyncLifetime
{
    private readonly B2CWebApiFixture _fixture;

    public B2CWebApiAuditLogTests(B2CWebApiFixture fixture, ITestOutputHelper logger)
    {
        _fixture = fixture;
        _fixture.SetTestOutputHelper(logger);
    }

    public static IEnumerable<object[]> GetB2CWebApiRequests()
    {
        return
        [
            [B2CWebApiRequests.CreateArchivedMessageGetDocumentRequest(), "DataHubAdministrator"],
            [B2CWebApiRequests.CreateArchivedMessageSearchRequest(), "DataHubAdministrator"],
            [B2CWebApiRequests.CreateOrchestrationsRequest(), "DataHubAdministrator"],
            [B2CWebApiRequests.CreateOrchestrationRequest(), "DataHubAdministrator"],
            [B2CWebApiRequests.CreateOrchestrationTerminateRequest(), "DataHubAdministrator"],
            [B2CWebApiRequests.CreateRequestAggregatedMeasureDataRequest(), ActorRole.EnergySupplier.Name],
            [B2CWebApiRequests.CreateRequestWholesaleSettlementRequest(), ActorRole.EnergySupplier.Name],
        ];
    }

    public Task InitializeAsync()
    {
        _fixture.AuditLogMockServer.ResetCallLogs();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _fixture.SetTestOutputHelper(null);
        return Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(GetB2CWebApiRequests))]
    public async Task B2CWebApiRequest_WhenRequestPerformed_AuditLogRequestIsSent(HttpRequestMessage request, string actorRole)
    {
        // Arrange
        request.Headers.Authorization = await _fixture
            .OpenIdJwtManager
            .JwtProvider
            .CreateInternalTokenAuthenticationHeaderAsync(
                roles: [
                    "request-wholesale-settlement:view",
                    "request-aggregated-measured-data:view",
                    "calculations:manage",
                ],
                extraClaims: [
                    new("actornumber", "1234567890123"),
                    new("marketroles", actorRole),
                ]);

        // Act
        await _fixture.WebApiClient.SendAsync(request);

        // Assert
        var auditLogCalls = _fixture.AuditLogMockServer.GetAuditLogIngestionCalls();

        var auditLogCall = auditLogCalls.Should()
            .ContainSingle()
            .Subject;

        using var assertionScope = new AssertionScope();

        // => Ensure that the audit log request was successful
        auditLogCall.Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

        // => Ensure that the audit log request contains a body
        auditLogCall.Request.Body.Should().NotBeNull();

        // => Ensure that the audit log request body can be deserialized to an instance of AuditLogRequestBody
        var deserializeBody = () =>
            JsonSerializer.Deserialize<AuditLogRequestBody>(auditLogCall.Request.Body ?? string.Empty);

        deserializeBody.Should().NotThrow()
            .Subject.Should().NotBeNull();
    }
}
