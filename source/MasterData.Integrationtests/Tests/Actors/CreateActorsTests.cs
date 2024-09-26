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

using System.Diagnostics.CodeAnalysis;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Application.Actors;
using Energinet.DataHub.EDI.MasterData.IntegrationTests.Fixture;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.MasterData.IntegrationTests.Tests.Actors;

[Collection(nameof(MasterDataTestCollectionFixture))]
public class CreateActorsTests : MasterDataTestBase
{
    [NotNull]
    private readonly IMasterDataClient? _masterDataClient;
    [NotNull]
    private readonly IDatabaseConnectionFactory? _connectionFactory;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public CreateActorsTests(MasterDataFixture masterDataFixture, ITestOutputHelper testOutputHelper)
        : base(masterDataFixture, testOutputHelper)
    {
        SetupServiceCollection();
        _masterDataClient = Services.GetService<IMasterDataClient>();
        _connectionFactory = Services.GetService<IDatabaseConnectionFactory>();
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

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
