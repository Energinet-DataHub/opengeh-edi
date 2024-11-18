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

using Energinet.DataHub.Core.Outbox.Infrastructure.DbContext;
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.AuditLog.AuditLogOutbox;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.MasterData.IntegrationTests.Fixture;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.MasterData.IntegrationTests.Tests;

[Collection(nameof(MasterDataTestCollection))]
public class RemoveOldGridAreaOwnersWhenADayHasPassedTests : MasterDataTestBase
{
    private readonly IMasterDataClient _masterDataClient;

    public RemoveOldGridAreaOwnersWhenADayHasPassedTests(MasterDataFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        SetupServiceCollection();
        _masterDataClient = Services.GetRequiredService<IMasterDataClient>();
        var authenticatedActor = Services.GetRequiredService<AuthenticatedActor>();
        // Retention jobs does not have an authenticated actor, so we need to set it to null.
        authenticatedActor.SetAuthenticatedActor(null);
    }

    [Fact]
    public async Task Given_RetentionCleanUp_When_TwoGridOwnerOnSameArea_Then_RemoveOldest()
    {
        // Arrange
        var actorNumberOfExpectedOwner = ActorNumber.Create("9876543210987");
        var gridAreaCode = "303";
        var gridAreaOwner1 = new GridAreaOwnershipAssignedDto(
            gridAreaCode,
            Instant.FromUtc(2023, 10, 1, 0, 0, 0),
            ActorNumber.Create("1234567890123"),
            1);

        var gridAreaOwner2 = new GridAreaOwnershipAssignedDto(
            gridAreaCode,
            Instant.FromUtc(2023, 10, 2, 0, 0, 0),
            actorNumberOfExpectedOwner,
            2);

        await AddActorsToDatabaseAsync(new List<GridAreaOwnershipAssignedDto> { gridAreaOwner1, gridAreaOwner2 });

        var sut = Services.GetRequiredService<IDataRetention>();

        // Act
        await sut.CleanupAsync(CancellationToken.None);

        // Assert
        var owners = await GetGridAreaOwnersForGridArea(gridAreaCode);
        Assert.NotNull(owners);
        Assert.Equal(gridAreaOwner2.GridAreaOwner.Value, owners.Value);
    }

    [Fact]
    public async Task Given_RetentionCleanUp_When_NonExpiredGridOwners_Then_NoGridAreaOwnerIsRemoved()
    {
        // Arrange
        var actorNumberOfExpectedOwner = ActorNumber.Create("9876543210987");
        var gridAreaCode = "303";
        var gridAreaOwner1 = new GridAreaOwnershipAssignedDto(
            gridAreaCode,
            Instant.FromUtc(2023, 10, 4, 0, 0, 0),
            ActorNumber.Create("1234567890123"),
            1);

        var gridAreaOwner2 = new GridAreaOwnershipAssignedDto(
            gridAreaCode,
            Instant.FromUtc(2023, 10, 5, 0, 0, 0),
            actorNumberOfExpectedOwner,
            2);

        await AddActorsToDatabaseAsync(new List<GridAreaOwnershipAssignedDto> { gridAreaOwner1, gridAreaOwner2 });

        var sut = Services.GetRequiredService<IDataRetention>();

        // Act
        await sut.CleanupAsync(CancellationToken.None);

        // Assert
        var owners = await GetGridAreaOwnersForGridArea(gridAreaCode);
        // Assert.Equal(2, owners.Count);
    }

    [Fact]
    public async Task Given_RetentionCleanUp_When_MultipleOwnersOnMulitpleAreas_Then_AllGridAreasHasOneOwner()
    {
        // Arrange
        var gridAreaCode1 = "301";
        var gridAreaOwner1 = new GridAreaOwnershipAssignedDto(
            gridAreaCode1,
            Instant.FromUtc(2023, 10, 1, 0, 0, 0),
            ActorNumber.Create("1234567890123"),
            1);

        var gridAreaOwner2 = new GridAreaOwnershipAssignedDto(
            gridAreaCode1,
            Instant.FromUtc(2023, 10, 2, 0, 0, 0),
            ActorNumber.Create("9876543210987"),
            2);

        await AddActorsToDatabaseAsync(new List<GridAreaOwnershipAssignedDto> { gridAreaOwner1, gridAreaOwner2 });

        var gridAreaCode2 = "302";
        var gridAreaOwner3 = new GridAreaOwnershipAssignedDto(
            gridAreaCode2,
            Instant.FromUtc(2023, 10, 1, 0, 0, 0),
            ActorNumber.Create("1234567890123"),
            1);

        var gridAreaOwner4 = new GridAreaOwnershipAssignedDto(
            gridAreaCode2,
            Instant.FromUtc(2023, 10, 2, 0, 0, 0),
            ActorNumber.Create("9876543210987"),
            2);

        await AddActorsToDatabaseAsync(new List<GridAreaOwnershipAssignedDto> { gridAreaOwner3, gridAreaOwner4 });

        var sut = Services.GetRequiredService<IDataRetention>();

        // Act
        await sut.CleanupAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(await GetGridAreaOwnersForGridArea(gridAreaCode1));
        Assert.NotNull(await GetGridAreaOwnersForGridArea(gridAreaCode2));
    }

    [Fact]
    public async Task Given_RetentionCleanUp_When_GridAreaOwnerIsRemoved_Then_AuditLogIsBeingLogged()
    {
        // Arrange
        var gridAreaCode = "303";
        var gridAreaOwner = new GridAreaOwnershipAssignedDto(
            gridAreaCode,
            Instant.FromUtc(2023, 10, 1, 0, 0, 0),
            ActorNumber.Create("1234567890123"),
            1);

        await AddActorsToDatabaseAsync(new List<GridAreaOwnershipAssignedDto> { gridAreaOwner });

        var sut = Services.GetRequiredService<IDataRetention>();

        // Act
        await sut.CleanupAsync(CancellationToken.None);

        // Assert
        using var secondScope = Services.CreateScope();
        var outboxContext = secondScope.ServiceProvider.GetRequiredService<IOutboxContext>();
        var serializer = secondScope.ServiceProvider.GetRequiredService<ISerializer>();
        var outboxMessages = outboxContext.Outbox;
        var outboxMessage = outboxMessages.Should().NotBeEmpty().And.Subject.First();
        var payload = serializer.Deserialize<AuditLogOutboxMessageV1Payload>(outboxMessage.Payload);
        payload.Origin.Should().Be(nameof(ADayHasPassed));
        payload.AffectedEntityType.Should().Be(AuditLogEntityType.GridAreaOwner.Identifier);
    }

    private async Task AddActorsToDatabaseAsync(List<GridAreaOwnershipAssignedDto> gridAreaOwners)
    {
        foreach (var gao in gridAreaOwners)
        {
            await _masterDataClient!.UpdateGridAreaOwnershipAsync(gao, CancellationToken.None);
        }
    }

    private async Task<ActorNumber> GetGridAreaOwnersForGridArea(string gridAreaCode)
    {
        var gridAreaOwner = await _masterDataClient.GetGridOwnerForGridAreaCodeAsync(gridAreaCode, CancellationToken.None);
        return gridAreaOwner.ActorNumber;
    }
}
