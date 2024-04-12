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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Actors;

public class CreateActorsTests : TestBase
{
    private readonly IMasterDataClient _masterDataClient;
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public CreateActorsTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _masterDataClient = GetService<IMasterDataClient>();
        _connectionFactory = GetService<IDatabaseConnectionFactory>();
    }

    [Fact]
    public async Task Actor_is_created()
    {
        var createActorDto = CreateDto();

        await _masterDataClient.CreateActorIfNotExistAsync(createActorDto, CancellationToken.None);

        var actor = await GetActor();

        Assert.NotNull(actor);
        Assert.Equal(SampleData.ActorNumber, actor.ActorNumber);
        Assert.Equal(SampleData.ExternalId, actor.ExternalId);
    }

    [Fact]
    public async Task Actor_is_not_created_multiple_times()
    {
        var createActorDto1 = CreateDto();
        var createActorDto2 = CreateDto();
        var createActorDto3 = CreateDto();
        var createActorDto4 = CreateDto();

        await _masterDataClient.CreateActorIfNotExistAsync(createActorDto1, CancellationToken.None);
        await _masterDataClient.CreateActorIfNotExistAsync(createActorDto2, CancellationToken.None);
        await _masterDataClient.CreateActorIfNotExistAsync(createActorDto3, CancellationToken.None);
        await _masterDataClient.CreateActorIfNotExistAsync(createActorDto4, CancellationToken.None);

        var actors = (await GetAllActors()).ToList();

        Assert.Single(actors);
        Assert.Equal(SampleData.ActorNumber, actors.First().ActorNumber);
        Assert.Equal(SampleData.ExternalId, actors.First().ExternalId);
    }

    private static CreateActorDto CreateDto()
    {
        return new CreateActorDto(SampleData.ExternalId, ActorNumber.Create(SampleData.ActorNumber));
    }

    private async Task<Actor?> GetActor()
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT Id, ActorNumber, ExternalId FROM [dbo].[Actor] WHERE ExternalId = '{SampleData.ExternalId}' AND ActorNumber = '{SampleData.ActorNumber}'";
        return await connection.QuerySingleOrDefaultAsync<Actor>(sql);
    }

    private async Task<IEnumerable<Actor>> GetAllActors()
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql =
            $"SELECT Id, ActorNumber, ExternalId " +
            $"FROM [dbo].[Actor] " +
            $"WHERE ExternalId = '{SampleData.ExternalId}' " +
            $"AND ActorNumber = '{SampleData.ActorNumber}'";

        return await connection.QueryAsync<Actor>(sql);
    }

#pragma warning disable
    public record Actor(Guid Id, string ActorNumber, string ExternalId);
#pragma warning restore
}
