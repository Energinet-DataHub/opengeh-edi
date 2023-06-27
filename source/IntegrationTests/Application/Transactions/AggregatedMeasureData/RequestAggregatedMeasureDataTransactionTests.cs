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

using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Dapper;
using IntegrationTests.Application.IncomingMessages;
using IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class RequestAggregatedMeasureDataTransactionTests : TestBase
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

    public RequestAggregatedMeasureDataTransactionTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _databaseConnectionFactory = GetService<IDatabaseConnectionFactory>();
    }

    [Fact]
    public async Task Aggregated_measure_data_process_is_created()
    {
        var incomingMessage = MessageBuilder()
            .Build();

        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
        var process = await GetProcess(incomingMessage.MessageHeader.SenderId);
        Assert.NotNull(process);
    }

    private static RequestAggregatedMeasureDataMessageBuilder MessageBuilder()
    {
        return new RequestAggregatedMeasureDataMessageBuilder();
    }

    private async Task<object?> GetProcess(string senderId)
    {
        using var connection = await _databaseConnectionFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        return connection.QuerySingle(
            $"SELECT * FROM dbo.AggregatedMeasureDataProcesses WHERE RequestedByActorId = @SenderId",
            new
            {
                @SenderId = senderId,
            });
    }
}
