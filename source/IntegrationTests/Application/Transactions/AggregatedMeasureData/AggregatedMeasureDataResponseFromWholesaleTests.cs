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
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Serialization;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.AggregationResult;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common;
using Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using NodaTime.Extensions;
using Xunit;
using Xunit.Categories;
using GridAreaDetails = Energinet.DataHub.EDI.Domain.Transactions.Aggregations.GridAreaDetails;
using Period = Energinet.DataHub.EDI.Domain.Transactions.Aggregations.Period;
using Point = Energinet.DataHub.EDI.Domain.Transactions.Aggregations.Point;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class AggregatedMeasureDataResponseFromWholesaleTests : TestBase
{
    private readonly B2BContext _b2BContext;

    public AggregatedMeasureDataResponseFromWholesaleTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _b2BContext = GetService<B2BContext>();
    }

    [Fact]
    public async Task Aggregated_measure_data_response_was_accepted()
    {
        // Arrange
        var expectedOutgoingMessages = 1;
        var marketMessage = MessageBuilder().Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var process = GetProcess(marketMessage.SenderNumber);
        process!.WasSentToWholesale();
        var acceptedAggregation = CreateAcceptedAggregation();

        // Act
        process.IsAccepted(acceptedAggregation, new AggregationResultXmlDocumentWriter(new MessageRecordParser(new Serializer())));

        // Assert
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Accepted);
        AssertOutgoingMessageCreated(process, expectedOutgoingMessages);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_accepted_will_only_be_processed_once()
    {
        // Arrange
        var expectedOutgoingMessages = 1;
        var marketMessage = MessageBuilder().Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var process = GetProcess(marketMessage.SenderNumber);
        process!.WasSentToWholesale();
        var acceptedAggregation = CreateAcceptedAggregation();

        // Act
        process.IsAccepted(acceptedAggregation, new AggregationResultXmlDocumentWriter(new MessageRecordParser(new Serializer())));
        process.IsAccepted(acceptedAggregation, new AggregationResultXmlDocumentWriter(new MessageRecordParser(new Serializer())));

        // Assert
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Accepted);
        AssertOutgoingMessageCreated(process, expectedOutgoingMessages);
    }

    [Fact]
    public async Task Aggregated_measure_data_response_was_rejected()
    {
        // Arrange
        var expectedOutgoingMessage = 1;
        var marketMessage = MessageBuilder().Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var process = GetProcess(marketMessage.SenderNumber);
        process!.WasSentToWholesale();
        var rejectedRequest = CreateRejectRequest();

        // Act
        process.IsRejected(rejectedRequest);

        // Assert
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Rejected);
        AssertOutgoingMessageCreated(process, expectedOutgoingMessage);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_rejected_will_only_be_processed_once()
    {
        // Arrange
        var expectedOutgoingMessage = 1;
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
        AssertOutgoingMessageCreated(process, expectedOutgoingMessage);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _b2BContext.Dispose();
    }

    private static RequestAggregatedMeasureDataMarketDocumentBuilder MessageBuilder()
    {
        return new RequestAggregatedMeasureDataMarketDocumentBuilder();
    }

    private static Aggregation CreateAcceptedAggregation()
    {
        var points = Array.Empty<Point>();

        return new Aggregation(
            points,
            MeteringPointType.Consumption.Name,
            MeasurementUnit.Kwh.Name,
            Resolution.Hourly.Name,
            new Period(DateTimeOffset.UtcNow.ToInstant(), DateTimeOffset.UtcNow.AddHours(1).ToInstant()),
            SettlementType.NonProfiled.Name,
            BusinessReason.BalanceFixing.Name,
            new ActorGrouping("1234567891911", null),
            new GridAreaDetails("805", "1234567891045"));
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

    private static void AssertOutgoingMessageCreated(AggregatedMeasureDataProcess process, int expectedOutgoingMessages)
    {
        var messages = typeof(AggregatedMeasureDataProcess).GetField("_messages", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(process) as IReadOnlyCollection<OutgoingMessage>;
        Assert.NotNull(messages);
        Assert.Equal(expectedOutgoingMessages, messages.Count);
    }

    private AggregatedMeasureDataProcess? GetProcess(string senderNumber)
    {
        return _b2BContext.AggregatedMeasureDataProcesses
            .ToList()
            .FirstOrDefault(x => x.RequestedByActorId.Value == senderNumber);
    }
}
