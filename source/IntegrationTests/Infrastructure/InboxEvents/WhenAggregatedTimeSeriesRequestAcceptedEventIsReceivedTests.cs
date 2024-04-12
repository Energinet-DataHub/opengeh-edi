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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.InboxEvents;

public class WhenAggregatedTimeSeriesRequestAcceptedEventIsReceivedTests : TestBase
{
    private const string GridAreaCode = "244";
    private readonly string _eventType = nameof(AggregatedTimeSeriesRequestAccepted);
    private readonly Guid _referenceId = Guid.NewGuid();
    private readonly string _eventId = Guid.NewGuid().ToString();
    private readonly InboxEventsProcessor _processor;
    private readonly AggregatedTimeSeriesRequestAccepted _aggregatedTimeSeriesRequestAcceptedResponse;
    private readonly GridAreaBuilder _gridAreaBuilder = new();

    public WhenAggregatedTimeSeriesRequestAcceptedEventIsReceivedTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _processor = GetService<InboxEventsProcessor>();
        _aggregatedTimeSeriesRequestAcceptedResponse = CreateResponseFromWholeSale();
    }

    [Fact]
    public async Task Event_is_marked_as_processed_when_a_handler_has_handled_it_successfully()
    {
        await _gridAreaBuilder
            .WithGridAreaCode(GridAreaCode)
            .StoreAsync(GetService<IMasterDataClient>());

        RegisterInboxEvent();

        await _processor.ProcessEventsAsync(CancellationToken.None);

        TestAggregatedTimeSeriesRequestAcceptedHandlerSpy.AssertExpectedNotifications(_aggregatedTimeSeriesRequestAcceptedResponse);
        await EventIsMarkedAsProcessedAsync(_eventId);
    }

    private static AggregatedTimeSeriesRequestAccepted CreateResponseFromWholeSale()
    {
        var quantity = new DecimalValue() { Units = 12345, Nanos = 123450000, };
        var point = new TimeSeriesPoint()
        {
            Quantity = quantity,
            Time = new Timestamp() { Seconds = 1, },
        };
        point.QuantityQualities.Add(QuantityQuality.Estimated);

        var series = new Series()
        {
            GridArea = GridAreaCode,
            QuantityUnit = QuantityUnit.Kwh,
            TimeSeriesPoints = { point },
            TimeSeriesType = TimeSeriesType.Production,
            Resolution = Resolution.Pt15M,
            CalculationResultVersion = 1,
            Period = new Period()
            {
                StartOfPeriod = Timestamp.FromDateTimeOffset(DateTimeOffset.Now),
                EndOfPeriod = Timestamp.FromDateTimeOffset(DateTimeOffset.Now.Add(TimeSpan.FromDays(5))),
            },
        };

        var timeSerie = new AggregatedTimeSeriesRequestAccepted();
        timeSerie.Series.Add(series);
        return timeSerie;
    }

    private void RegisterInboxEvent()
    {
        var context = GetService<ProcessContext>();
        context.ReceivedInboxEvents.Add(new ReceivedInboxEvent(
            _eventId,
            _eventType,
            _referenceId,
            _aggregatedTimeSeriesRequestAcceptedResponse.ToByteArray(),
            GetService<ISystemDateTimeProvider>().Now()));
        context.SaveChanges();
    }

    private async Task EventIsMarkedAsProcessedAsync(string eventId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var isProcessed = await connection.QueryFirstOrDefaultAsync($"SELECT * FROM dbo.ReceivedInboxEvents WHERE Id = @EventId AND ProcessedDate IS NOT NULL", new { EventId = eventId, });
        Assert.NotNull(isProcessed);
    }
}
