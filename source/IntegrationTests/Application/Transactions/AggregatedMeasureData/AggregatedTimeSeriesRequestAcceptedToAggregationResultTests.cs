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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Categories;
using Enum = System.Enum;
using Point = Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage.Point;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1204:Static elements should appear before instance elements",
    Justification = "Test class")]
public sealed class AggregatedTimeSeriesRequestAcceptedToAggregationResultTests : TestBase
{
    private readonly GridAreaBuilder _gridAreaBuilder = new();
    private readonly ProcessContext _processContext;
    private readonly InboxEventReceiver _inboxEventReceiver;
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly IMasterDataClient _masterDataClient;
    private readonly IFileStorageClient _fileStorageClient;

    public AggregatedTimeSeriesRequestAcceptedToAggregationResultTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _processContext = GetService<ProcessContext>();
        _inboxEventReceiver = GetService<InboxEventReceiver>();
        _databaseConnectionFactory = GetService<IDatabaseConnectionFactory>();
        _masterDataClient = GetService<IMasterDataClient>();
        _fileStorageClient = GetService<IFileStorageClient>();
    }

    public static IEnumerable<object[]> ExpectedQuantityQualityMappings()
    {
        // First element is the QuantityQuality from the message,
        // the second element is the expected CalculatedQuantityQuality
        return new[]
        {
            new object[] { QuantityQuality.Missing, CalculatedQuantityQuality.NotAvailable },
            new object[] { QuantityQuality.Estimated, CalculatedQuantityQuality.Estimated },
            new object[] { QuantityQuality.Measured, CalculatedQuantityQuality.Measured },
            new object[] { QuantityQuality.Incomplete, CalculatedQuantityQuality.Incomplete },
            new object[] { QuantityQuality.Calculated, CalculatedQuantityQuality.Calculated },
        };
    }

    [Fact]
    public async Task Unspecified_quantity_quality_is_a_technical_artefact_and_produces_a_mapping_error()
    {
        var act = async () =>
        {
            await AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(QuantityQuality.Unspecified);
        };

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Sequence contains no elements");
    }

    [Theory]
    [MemberData(nameof(ExpectedQuantityQualityMappings))]
    public async Task Quantity_quality_is_mapped_correctly(
        QuantityQuality quantityQuality,
        CalculatedQuantityQuality calculatedQuantityQuality)
    {
        // Arrange & Act
        var result = await AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(quantityQuality);

        // Assert
        result
            .HasMessageRecordValue<TimeSeries, IReadOnlyList<Point>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<TimeSeries, Point>(
                series => series.Point[0],
                point =>
                {
                    point.Should().NotBeNull();
                    point!.QuantityQuality.Should().Be(calculatedQuantityQuality);
                });
    }

    [Fact]
    public void Ensure_all_enums_are_part_of_expected_mapping()
    {
        ExpectedQuantityQualityMappings()
            .SelectMany((q, _) => q)
            .Cast<QuantityQuality>()
            .Should()
            .Contain(QuantityQualities().SelectMany(q => q).Cast<QuantityQuality>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _processContext.Dispose();
    }

    private static IEnumerable<object[]> QuantityQualities()
    {
        return Enum.GetValues(typeof(QuantityQuality))
            .Cast<QuantityQuality>()
            .Where(quality => quality != QuantityQuality.Unspecified)
            .Cast<object>()
            .Select(quality => new[] { quality });
    }

    private async Task<AssertOutgoingMessage> AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(
        QuantityQuality quantityQuality)
    {
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .StoreAsync(_masterDataClient);

        var process = BuildProcess();
        var requestAccepted = CreateEventWithQuantityQuality(quantityQuality);

        await _inboxEventReceiver
            .ReceiveAsync(
                Guid.NewGuid().ToString(),
                nameof(AggregatedTimeSeriesRequestAccepted),
                process.ProcessId.Id,
                requestAccepted.ToByteArray());

        await HavingReceivedInboxEventAsync(
            nameof(AggregatedTimeSeriesRequestAccepted),
            requestAccepted,
            process.ProcessId.Id);

        return await OutgoingMessageAsync();
    }

    private static AggregatedTimeSeriesRequestAccepted CreateEventWithQuantityQuality(QuantityQuality quantityQuality)
    {
        var timeSeriesPoint = new TimeSeriesPoint
        {
            Quantity = new DecimalValue { Units = 1, Nanos = 1 },
            QuantityQuality = quantityQuality,
            Time = new Timestamp { Seconds = 1, Nanos = 1 },
        };

        var series = new Series
        {
            GridArea = SampleData.GridAreaCode,
            TimeSeriesType = TimeSeriesType.Production,
            QuantityUnit = QuantityUnit.Kwh,
            Resolution = Resolution.Pt1H,
        };
        series.TimeSeriesPoints.Add(timeSeriesPoint);

        var requestAccepted = new AggregatedTimeSeriesRequestAccepted();
        requestAccepted.Series.Add(series);

        return requestAccepted;
    }

    private AggregatedMeasureDataProcess BuildProcess()
    {
        var process = new AggregatedMeasureDataProcess(
            ProcessId.New(),
            BusinessTransactionId.Create(Guid.NewGuid().ToString()),
            SampleData.ReceiverNumber,
            SampleData.BalanceResponsibleParty.Code,
            BusinessReason.BalanceFixing,
            MeteringPointType.Production.Code,
            null,
            SampleData.StartOfPeriod,
            SampleData.EndOfPeriod,
            SampleData.GridAreaCode,
            null,
            SampleData.ReceiverNumber.Value,
            null);

        process.WasSentToWholesale();
        _processContext.AggregatedMeasureDataProcesses.Add(process);
        _processContext.SaveChanges();
        return process;
    }

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync()
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.NotifyAggregatedMeasureData.Name,
            BusinessReason.BalanceFixing.Name,
            MarketRole.BalanceResponsibleParty,
            _databaseConnectionFactory,
            _fileStorageClient);
    }
}
