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
using System.Reflection;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using FluentAssertions;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;
using RejectReason = Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.RejectReason;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class EnergyResultResponseFromWholesaleTests : TestBase
{
    private readonly ProcessContext _processContext;

    public EnergyResultResponseFromWholesaleTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
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
        process!.SendToWholesale();
        var aggregationResultMessage = CreateAcceptedEnergyResultMessageDtoMessage(process);

        // Act
        process.IsAccepted(new List<AcceptedEnergyResultMessageDto> { aggregationResultMessage });

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
        process!.SendToWholesale();
        var firstAggregationResultMessage = CreateAcceptedEnergyResultMessageDtoMessage(process);
        var secondAggregationResultMessage = CreateAcceptedEnergyResultMessageDtoMessage(process, gridarea: "808");

        // Act
        process.IsAccepted(new List<AcceptedEnergyResultMessageDto> { firstAggregationResultMessage, secondAggregationResultMessage });

        // Assert
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Accepted);

        process.DomainEvents.Should().HaveCount(3);
        process.DomainEvents.Select(de => de.GetType())
            .Distinct()
            .Should()
            .BeEquivalentTo(
                new[]
                {
                    typeof(EnqueueAcceptedEnergyResultMessageEvent),
                    typeof(NotifyWholesaleThatAggregatedMeasureDataIsRequested),
                });

        process.DomainEvents.Where(de => de is EnqueueAcceptedEnergyResultMessageEvent).Should().HaveCount(2);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_accepted_will_only_be_processed_once()
    {
        // Arrange
        var marketMessage = MessageBuilder().Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var process = GetProcess(marketMessage.SenderNumber);
        process!.SendToWholesale();
        var aggregationResultMessage = CreateAcceptedEnergyResultMessageDtoMessage(process);

        // Act
        process.IsAccepted(new List<AcceptedEnergyResultMessageDto> { aggregationResultMessage });
        process.IsAccepted(new List<AcceptedEnergyResultMessageDto> { aggregationResultMessage });

        // Assert
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Accepted);
        Assert.Contains(process.DomainEvents, x => x is EnqueueAcceptedEnergyResultMessageEvent);
    }

    [Fact]
    public async Task Aggregated_measure_data_response_was_rejected()
    {
        // Arrange
        var marketMessage = MessageBuilder().Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var process = GetProcess(marketMessage.SenderNumber);
        process!.SendToWholesale();
        var rejectedRequest = CreateRejectRequest();

        // Act
        process.IsRejected(rejectedRequest);

        // Assert
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Rejected);
        Assert.Contains(process.DomainEvents, x => x is EnqueueRejectedEnergyResultMessageEvent);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_rejected_will_only_be_processed_once()
    {
        // Arrange
        var marketMessage = MessageBuilder().Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var process = GetProcess(marketMessage.SenderNumber);
        process!.SendToWholesale();
        var rejectedRequest = CreateRejectRequest();

        // Act
        process.IsRejected(rejectedRequest);
        process.IsRejected(rejectedRequest);

        // Assert
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Rejected);
        Assert.Contains(process.DomainEvents, x => x is EnqueueRejectedEnergyResultMessageEvent);
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

    private static AcceptedEnergyResultMessageDto CreateAcceptedEnergyResultMessageDtoMessage(
        AggregatedMeasureDataProcess process,
        string gridarea = "805")
    {
        var points = new List<AcceptedEnergyResultMessagePoint>()
        {
            new(1, 2, CalculatedQuantityQuality.Calculated, process.StartOfPeriod),
            new(2, 3, CalculatedQuantityQuality.Calculated, process.EndOfPeriod!),
        };

        return AcceptedEnergyResultMessageDto.Create(
            process.RequestedByActorId,
            ActorRole.FromCode(process.RequestedByActorRoleCode),
            process.ProcessId.Id,
            Guid.NewGuid().ToString(),
            gridarea,
            MeteringPointType.Production.Name,
            process.SettlementMethod,
            MeasurementUnit.Kwh.Name,
            Resolution.QuarterHourly.Name,
            process.EnergySupplierId,
            process.BalanceResponsibleId,
            new Period(
                InstantPattern.General.Parse(process.StartOfPeriod).Value,
                InstantPattern.General.Parse(process.EndOfPeriod!).Value),
            points.ToList().AsReadOnly(),
            process.BusinessReason.Name,
            1,
            settlementVersion: process.SettlementVersion?.Name);
    }

    private static RejectedAggregatedMeasureDataRequest CreateRejectRequest()
    {
        var rejectReasons = new List<RejectReason>()
        {
            new("E86", "Invalid request"),
        };
        return new RejectedAggregatedMeasureDataRequest(Guid.NewGuid().ToString(), rejectReasons, BusinessReason.BalanceFixing);
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
