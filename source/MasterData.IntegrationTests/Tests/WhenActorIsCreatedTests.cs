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

using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.MasterData.IntegrationTests.Fixture;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.MasterData.IntegrationTests.Tests;

[Collection(nameof(MasterDataTestCollection))]
public class WhenActorIsCreatedTests : MasterDataTestBase
{
    private readonly IMasterDataClient _masterDataClient;
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public WhenActorIsCreatedTests(MasterDataFixture masterDataFixture, ITestOutputHelper testOutputHelper)
        : base(masterDataFixture, testOutputHelper)
    {
        SetupServiceCollection();
        _masterDataClient = Services.GetRequiredService<IMasterDataClient>();
        _connectionFactory = Services.GetRequiredService<IDatabaseConnectionFactory>();
    }

    private static string ActorNumber => "5148796574821";

    private static string ActorClientId => Guid.Parse("9222905B-8B02-4D8B-A2C1-3BD51B1AD8D9").ToString();

    [Fact]
    public async Task Given_ActorIsCreated_When_ActorDoesNotExists_Then_ActorIsCreated()
    {
        var createActorDto = CreateDto();

        await _masterDataClient!.CreateActorIfNotExistAsync(createActorDto, CancellationToken.None);

        var actor = await GetActor();

        Assert.NotNull(actor);
        Assert.Equal(ActorNumber, actor.ActorNumber);
        Assert.Equal(ActorClientId, actor.ExternalId);
    }

    [Fact]
    public async Task Given_ActorIsCreated_When_ActorAlreadyExists_Then_IsNotCreatedMultipleTimes()
    {
        var createActorDto1 = CreateDto();
        var createActorDto2 = CreateDto();
        var createActorDto3 = CreateDto();
        var createActorDto4 = CreateDto();

        await _masterDataClient!.CreateActorIfNotExistAsync(createActorDto1, CancellationToken.None);
        await _masterDataClient.CreateActorIfNotExistAsync(createActorDto2, CancellationToken.None);
        await _masterDataClient.CreateActorIfNotExistAsync(createActorDto3, CancellationToken.None);
        await _masterDataClient.CreateActorIfNotExistAsync(createActorDto4, CancellationToken.None);

        var actors = (await GetAllActors()).ToList();

        Assert.Single(actors);
        Assert.Equal(ActorNumber, actors.First().ActorNumber);
        Assert.Equal(ActorClientId, actors.First().ExternalId);
    }

    private static CreateActorDto CreateDto()
    {
        return new CreateActorDto(ActorClientId, BuildingBlocks.Domain.Models.ActorNumber.Create(ActorNumber));
    }

    private async Task<Actor?> GetActor()
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT Id, ActorNumber, ExternalId FROM [dbo].[Actor] WHERE ExternalId = '{ActorClientId}' AND ActorNumber = '{ActorNumber}'";
        return await connection.QuerySingleOrDefaultAsync<Actor>(sql);
    }

    private async Task<IEnumerable<Actor>> GetAllActors()
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql =
            $"SELECT Id, ActorNumber, ExternalId " +
            $"FROM [dbo].[Actor] " +
            $"WHERE ExternalId = '{ActorClientId}' " +
            $"AND ActorNumber = '{ActorNumber}'";

        return await connection.QueryAsync<Actor>(sql);
    }

#pragma warning disable
    public record Actor(Guid Id, string ActorNumber, string ExternalId);
#pragma warning restore
}
