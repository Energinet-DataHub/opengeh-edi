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
using System.Threading.Tasks;
using Dapper;
using JetBrains.Annotations;
using MediatR;
using Messaging.Application.Actors;
using Messaging.Application.Configuration.DataAccess;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Actors;

public class CreateActorsTests : TestBase
{
    private readonly IMediator _mediator;
    private readonly IDbConnectionFactory _connectionFactory;

    public CreateActorsTests([NotNull] DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _mediator = GetService<IMediator>();
        _connectionFactory = GetService<IDbConnectionFactory>();
    }

    [Fact]
    public async Task Actor_is_created()
    {
        var command = CreateCommand();

        await _mediator.Send(command).ConfigureAwait(false);

        var actor = await GetActor().ConfigureAwait(false);

        Assert.NotNull(actor);
        Assert.Equal(SampleData.ActorId, actor.Id.ToString());
        Assert.Equal(SampleData.IdentificationNumber, actor.IdentificationNumber);
    }

    private static CreateActor CreateCommand()
    {
        return new CreateActor(SampleData.ActorId, SampleData.IdentificationNumber);
    }

    private async Task<Actor> GetActor()
    {
        var sql = $"SELECT Id, IdentificationNUmber FROM [b2b].[Actor] WHERE Id = '{SampleData.ActorId}'";
        return await _connectionFactory.GetOpenConnection().QuerySingleOrDefaultAsync<Actor>(sql).ConfigureAwait(false);
    }

#pragma warning disable
    public record Actor(Guid Id, string IdentificationNumber);
#pragma warning restore
}
