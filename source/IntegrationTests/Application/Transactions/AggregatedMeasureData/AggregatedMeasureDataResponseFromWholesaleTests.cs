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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using NodaTime.Text;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class AggregatedMeasureDataResponseFromWholesaleTests : TestBase
{
    private readonly ProcessContext _processContext;

    public AggregatedMeasureDataResponseFromWholesaleTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _processContext = GetService<ProcessContext>();
    }

    [Fact]
    public async Task Aggregated_measure_data_response_was_accepted()
    {
        // Arrange
        var marketMessage = MessageBuilder().Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var process = GetProcess(marketMessage.SenderNumber);
        process!.WasSentToWholesale();
        var acceptedAggregation = CreateAggregatedTimeSerie(process.StartOfPeriod, process.EndOfPeriod!);

        // Act
        process.IsAccepted(new List<AggregatedTimeSerie> { acceptedAggregation });

        // Assert
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Accepted);
    }

    [Fact]
    public async Task Aggregated_measure_data_response_was_accepted_with_two_series()
    {
        // Arrange
        var marketMessage = MessageBuilder().Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var process = GetProcess(marketMessage.SenderNumber);
        process!.WasSentToWholesale();
        var firstAcceptedAggregation = CreateAggregatedTimeSerie(process.StartOfPeriod, process.EndOfPeriod!);
        var secondAcceptedAggregation = CreateAggregatedTimeSerie(process.StartOfPeriod, process.EndOfPeriod!, gridarea: "808");

        // Act
        process.IsAccepted(new List<AggregatedTimeSerie> { firstAcceptedAggregation, secondAcceptedAggregation });

        // Assert
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Accepted);
        Assert.Contains(process.DomainEvents, x => x is EnqueueMessageEvent);
        Assert.All(process.DomainEvents, x
            => Assert.Equal(typeof(EnqueueMessageEvent), x.GetType()));
        Assert.Equal(2, process.DomainEvents.Count);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_accepted_will_only_be_processed_once()
    {
        // Arrange
        var marketMessage = MessageBuilder().Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var process = GetProcess(marketMessage.SenderNumber);
        process!.WasSentToWholesale();
        var acceptedAggregation = CreateAggregatedTimeSerie(process.StartOfPeriod, process.EndOfPeriod!);

        // Act
        process.IsAccepted(new List<AggregatedTimeSerie> { acceptedAggregation });
        process.IsAccepted(new List<AggregatedTimeSerie> { acceptedAggregation });

        // Assert
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Accepted);
        Assert.Contains(process.DomainEvents, x => x is EnqueueMessageEvent);
    }

    [Fact]
    public async Task Aggregated_measure_data_response_was_rejected()
    {
        // Arrange
        var marketMessage = MessageBuilder().Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var process = GetProcess(marketMessage.SenderNumber);
        process!.WasSentToWholesale();
        var rejectedRequest = CreateRejectRequest();

        // Act
        process.IsRejected(rejectedRequest);

        // Assert
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Rejected);
        Assert.Contains(process.DomainEvents, x => x is EnqueueMessageEvent);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_rejected_will_only_be_processed_once()
    {
        // Arrange
        var marketMessage = MessageBuilder().Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var process = GetProcess(marketMessage.SenderNumber);
        process!.WasSentToWholesale();
        var rejectedRequest = CreateRejectRequest();

        // Act
        process.IsRejected(rejectedRequest);
        process.IsRejected(rejectedRequest);

        // Assert
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Rejected);
        Assert.Contains(process.DomainEvents, x => x is EnqueueMessageEvent);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _processContext.Dispose();
    }

    private static RequestAggregatedMeasureDataMarketDocumentBuilder MessageBuilder()
    {
        return new RequestAggregatedMeasureDataMarketDocumentBuilder();
    }

    private static AggregatedTimeSerie CreateAggregatedTimeSerie(string start, string end, string gridarea = "805")
    {
        var points = new List<Point>()
        {
            new(1, 2, CalculatedQuantityQuality.Calculated, start.ToString()),
            new(2, 3, CalculatedQuantityQuality.Calculated, end.ToString()),
        };

        return new AggregatedTimeSerie(
            points,
            MeteringPointType.Consumption.Name,
            MeasurementUnit.Kwh.Name,
            Resolution.Hourly.Name,
            new GridAreaDetails(gridarea, "1234567891045"),
            1,
            InstantPattern.General.Parse(start).Value,
            InstantPattern.General.Parse(end).Value);
    }

    private static RejectedAggregatedMeasureDataRequest CreateRejectRequest()
    {
        var rejectReasons = new List<RejectReason>()
        {
            new("E86", "Invalid request"),
        };
        return new RejectedAggregatedMeasureDataRequest(rejectReasons, BusinessReason.BalanceFixing);
    }

    private static void AssertProcessState(AggregatedMeasureDataProcess process, AggregatedMeasureDataProcess.State state)
    {
        var processState = typeof(AggregatedMeasureDataProcess).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(process);
        Assert.Equal(state, processState);
    }

    private AggregatedMeasureDataProcess? GetProcess(string senderNumber)
    {
        return _processContext.AggregatedMeasureDataProcesses
            .ToList()
            .FirstOrDefault(x => x.RequestedByActorId.Value == senderNumber);
    }
}
