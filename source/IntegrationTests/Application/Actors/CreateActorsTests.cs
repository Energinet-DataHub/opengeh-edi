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
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.Application.Actors;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Common.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using MediatR;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Actors;

public class CreateActorsTests : TestBase
{
    private readonly IMediator _mediator;
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public CreateActorsTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _mediator = GetService<IMediator>();
        _connectionFactory = GetService<IDatabaseConnectionFactory>();
    }

    [Fact]
    public async Task Actor_is_created()
    {
        var command = CreateCommand();

        await _mediator.Send(command);

        var actor = await GetActor();

        Assert.NotNull(actor);
        Assert.Equal(SampleData.ActorNumber, actor.ActorNumber);
        Assert.Equal(SampleData.ExternalId, actor.ExternalId);
    }

    private static CreateActorCommand CreateCommand()
    {
        return new CreateActorCommand(SampleData.ExternalId, ActorNumber.Create(SampleData.ActorNumber));
    }

    private async Task<Actor> GetActor()
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT Id, ActorNumber, ExternalId FROM [dbo].[Actor] WHERE ExternalId = '{SampleData.ExternalId}' AND ActorNumber = '{SampleData.ActorNumber}'";
        return await connection.QuerySingleOrDefaultAsync<Actor>(sql);
    }

#pragma warning disable
    public record Actor(Guid Id, string ActorNumber, string ExternalId);
#pragma warning restore
}
