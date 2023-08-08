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

using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Domain.Transactions.AggregatedMeasureData;
using Infrastructure.Configuration.DataAccess;
using IntegrationTests.Application.IncomingMessages;
using IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class AggregatedMeasureDataRequestFromWholesaleTests : TestBase
{
    private readonly B2BContext _b2BContext;

    public AggregatedMeasureDataRequestFromWholesaleTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _b2BContext = GetService<B2BContext>();
    }

    [Fact]
    public async Task Aggregated_measure_data_process_is_created()
    {
        // Arrange
        var incomingMessage = MessageBuilder().Build();

        // Act
        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);

        // Assert
        var process = GetProcess(incomingMessage.MessageHeader.SenderId);
        Assert.Equal(process!.BusinessTransactionId.Id, incomingMessage.MarketActivityRecord.Id);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Initialized);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_was_send_to_wholesale()
    {
        // Arrange
        var incomingMessage = MessageBuilder().Build();
        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
        var process = GetProcess(incomingMessage.MessageHeader.SenderId);

        // Act
        process!.WasSentToWholesale();

        // Assert
        Assert.Equal(process.BusinessTransactionId.Id, incomingMessage.MarketActivityRecord.Id);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Sent);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _b2BContext.Dispose();
    }

    private static RequestAggregatedMeasureDataMessageBuilder MessageBuilder()
    {
        return new RequestAggregatedMeasureDataMessageBuilder();
    }

    private static void AssertProcessState(AggregatedMeasureDataProcess process, AggregatedMeasureDataProcess.State state)
    {
        var processState = typeof(AggregatedMeasureDataProcess).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(process);
        Assert.Equal(state, processState);
    }

    private AggregatedMeasureDataProcess? GetProcess(string senderId)
    {
        return _b2BContext.AggregatedMeasureDataProcesses
            .ToList()
            .FirstOrDefault(x => x.RequestedByActorId.Value == senderId);
    }
}
