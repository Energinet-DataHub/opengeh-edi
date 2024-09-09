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

using Energinet.DataHub.EDI.AuditLog;
using Energinet.DataHub.EDI.AuditLog.AuditLogOutbox;
using Energinet.DataHub.EDI.B2CWebApi.AppTests.Fixture;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.Outbox.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2CWebApi.AppTests.Tests;

[Collection(nameof(B2CWebApiCollectionFixture))]
public class B2CWebApiAuditLogTests : IAsyncLifetime
{
    private static readonly string _datahubAdministratorRole = "DataHubAdministrator";
    private static readonly Guid _datahubAdministratorActorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly string _datahubAdministratorActorNumber = "5790001330583";

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
            [B2CWebApiRequests.CreateArchivedMessageGetDocumentRequest(), _datahubAdministratorRole, AuditLogActivity.ArchivedMessagesGet],
            [B2CWebApiRequests.CreateArchivedMessageSearchRequest(), _datahubAdministratorRole, AuditLogActivity.ArchivedMessagesSearch],
            [B2CWebApiRequests.CreateOrchestrationsRequest(), _datahubAdministratorRole, AuditLogActivity.OrchestrationsSearch],
            [B2CWebApiRequests.CreateOrchestrationRequest(), _datahubAdministratorRole, AuditLogActivity.OrchestrationsGet],
            [B2CWebApiRequests.CreateOrchestrationTerminateRequest(), _datahubAdministratorRole, AuditLogActivity.OrchestrationsTerminate],
            [B2CWebApiRequests.CreateRequestAggregatedMeasureDataRequest(), ActorRole.EnergySupplier.Name, AuditLogActivity.RequestEnergyResults],
            [B2CWebApiRequests.CreateRequestWholesaleSettlementRequest(), ActorRole.EnergySupplier.Name, AuditLogActivity.RequestWholesaleResults],
        ];
    }

    public async Task InitializeAsync()
    {
        await using var context = _fixture.DatabaseManager.CreateDbContext<OutboxContext>();
        await context.Outbox.ExecuteDeleteAsync();
    }

    public Task DisposeAsync()
    {
        _fixture.SetTestOutputHelper(null);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate that the correct audit log request is sent when a B2C Web API request is performed, according to
    /// the audit log documentation at: https://github.com/Energinet-DataHub/opengeh-revision-log/blob/main/docs/documentation-for-submitting-audit-logs.md
    /// </summary>
    [Theory]
    [MemberData(nameof(GetB2CWebApiRequests))]
    public async Task B2CWebApiRequest_WhenRequestPerformed_CorrectAuditLogRequestAddedToOutbox(
        HttpRequestMessage request,
        string actorRole,
        AuditLogActivity expectedActivity)
    {
        // Arrange
        var serializer = new Serializer();

        // The OrchestrationsTerminate request will fail since there is no orchestration to terminate
        var checkRequestSuccess = expectedActivity != AuditLogActivity.OrchestrationsTerminate;

        var isAdministrator = actorRole == _datahubAdministratorRole;
        var expectedUserId = Guid.NewGuid();
        var expectedActorId = isAdministrator
            ? _datahubAdministratorActorId
            : Guid.NewGuid();

        string[] permissions =
        [
            "request-wholesale-settlement:view",
            "request-aggregated-measured-data:view",
            "calculations:manage",
        ];
        var expectedPermissions = string.Join(", ", permissions);

        request.Headers.Authorization = await _fixture
            .OpenIdJwtManager
            .JwtProvider
            .CreateInternalTokenAuthenticationHeaderAsync(
                userId: expectedUserId.ToString(),
                actorId: expectedActorId.ToString(),
                roles: permissions,
                extraClaims: [
                    new("actornumber", isAdministrator ? _datahubAdministratorActorNumber : "1234567890123"),
                    new("marketroles", actorRole),
                ]);

        // Act
        var response = await _fixture.WebApiClient.SendAsync(request);

        // Assert
        await using var outboxContext = _fixture.DatabaseManager.CreateDbContext<OutboxContext>();
        var outboxMessage = outboxContext.Outbox.SingleOrDefault();

        using (new AssertionScope())
        {
            if (checkRequestSuccess)
            {
                var ensureSuccess = () => response.EnsureSuccessStatusCodeWithLogAsync(_fixture.TestLogger);
                await ensureSuccess.Should().NotThrowAsync();
            }

            outboxMessage.Should().NotBeNull();
        }

        outboxMessage!.Type.Should().Be(AuditLogOutboxMessageV1.OutboxMessageType);
        outboxMessage.ShouldProcessNow(SystemClock.Instance).Should().BeTrue();
        var auditLogPayload = serializer.Deserialize<AuditLogOutboxMessageV1Payload>(outboxMessage.Payload);

        using var assertionScope = new AssertionScope();
        auditLogPayload!.LogId.Should().NotBeEmpty();
        auditLogPayload.UserId.Should().Be(expectedUserId);
        auditLogPayload.ActorId.Should().Be(expectedActorId);
        auditLogPayload.SystemId.Should().Be(Guid.Parse("688b2dca-7231-490f-a731-d7869d33fe5e")); // EDI subsystem id
        auditLogPayload.Permissions.Should().Be(expectedPermissions);
        auditLogPayload.OccuredOn.Should().NotBeNull();
        auditLogPayload.Activity.Should().Be(expectedActivity.Identifier);
        auditLogPayload.Origin.Should().Be(request.RequestUri?.AbsoluteUri);
        auditLogPayload.Payload.Should().NotBeNull();
        auditLogPayload.AffectedEntityType.Should().NotBeNullOrWhiteSpace();

        request.Dispose();
    }
}
