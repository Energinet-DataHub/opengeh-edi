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

using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MeteringPoints.IntegrationEvents.CreateMeteringPoint;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Infrastructure.MarketEvaluationPoints;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Infrastructure.MarketEvaluationPoints;

public class MarketEvaluationPointReadModelTests : TestBase
{
    private readonly MarketEvaluationPointReadModelHandler _readModelHandler;

    public MarketEvaluationPointReadModelTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _readModelHandler = GetService<MarketEvaluationPointReadModelHandler>();
    }

    [Fact]
    public async Task Grid_operator_id_is_registered_when_a_metering_point_is_created()
    {
        var @event = new MeteringPointCreated
        {
            Id = SampleData.MeteringPointId,
            GsrnNumber = SampleData.MeteringPointNumber,
            GridOperatorId = SampleData.GridOperatorId.ToString(),
        };

        await _readModelHandler.WhenAsync(@event).ConfigureAwait(false);

        var exists = await GetService<IDbConnectionFactory>()
            .GetOpenConnection()
            .ExecuteScalarAsync<bool>(
                "SELECT COUNT(*) FROM b2b.MarketEvaluationPoints WHERE Id = @Id AND MarketEvaluationPointNumber = @MarketEvaluationPointNumber AND GridOperatorId = @GridOperatorId",
                new
                {
                    Id = SampleData.MeteringPointId,
                    MarketEvaluationPointNumber = SampleData.MeteringPointNumber,
                    GridOperatorId = SampleData.GridOperatorId,
                })
            .ConfigureAwait(false);

        Assert.True(exists);
    }
}
