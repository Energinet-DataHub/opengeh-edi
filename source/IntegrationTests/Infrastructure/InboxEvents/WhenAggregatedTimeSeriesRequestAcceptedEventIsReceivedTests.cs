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
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Infrastructure.Transactions.Aggregations;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Period = Energinet.DataHub.Edi.Responses.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.InboxEvents;

public class WhenAggregatedTimeSeriesRequestAcceptedEventIsReceivedTests : TestBase
{
    private const string GridAreaCode = "244";
    private readonly string _eventType = nameof(AggregatedTimeSeriesRequestAccepted);
    private readonly Guid _referenceId = Guid.NewGuid();
    private readonly string _eventId = "1";
    private readonly InboxEventsProcessor _processor;
    private readonly AggregatedTimeSeriesRequestAcceptedEventMapper _aggregatedTimeSeriesRequestAcceptedEventMapper;
    private readonly AggregatedTimeSeriesRequestAccepted _aggregatedTimeSeriesRequestAcceptedResponse;
    private readonly GridAreaBuilder _gridAreaBuilder = new();

    public WhenAggregatedTimeSeriesRequestAcceptedEventIsReceivedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _processor = GetService<InboxEventsProcessor>();
        _aggregatedTimeSeriesRequestAcceptedEventMapper = GetService<AggregatedTimeSeriesRequestAcceptedEventMapper>();
        _aggregatedTimeSeriesRequestAcceptedResponse = CreateResponseFromWholeSale();
        RegisterInboxEvent();
    }

    [Fact]
    public async Task Event_is_marked_as_processed_when_a_handler_has_handled_it_successfully()
    {
        _gridAreaBuilder
            .WithGridAreaCode(GridArea)
            .Store(GetService<B2BContext>());

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
            QuantityQuality = QuantityQuality.Incomplete,
            Time = new Timestamp() { Seconds = 1, },
        };

        var period = new Period()
        {
            StartOfPeriod = new Timestamp() { Seconds = 1, },
            EndOfPeriod = new Timestamp() { Seconds = 2, },
            Resolution = Resolution.Pt15M,
        };

        return new AggregatedTimeSeriesRequestAccepted()
        {
            SettlementVersion = "0",
            GridArea = GridAreaCode,
            QuantityUnit = QuantityUnit.Kwh,
            Period = period,
            TimeSeriesPoints = { point },
            TimeSeriesType = TimeSeriesType.Production,
        };
    }

    private void RegisterInboxEvent()
    {
        var context = GetService<B2BContext>();
        context.ReceivedInboxEvents.Add(new ReceivedInboxEvent(
            _eventId,
            _eventType,
            _referenceId,
            ToJson(),
            GetService<ISystemDateTimeProvider>().Now()));
        context.SaveChanges();
    }

    private async Task EventIsMarkedAsProcessedAsync(string eventId)
    {
        var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var isProcessed = connection.ExecuteScalar<bool>($"SELECT COUNT(*) FROM dbo.ReceivedInboxEvents WHERE Id = @EventId AND ProcessedDate IS NOT NULL", new { EventId = eventId, });
        Assert.True(isProcessed);
    }

    private string ToJson()
    {
        return _aggregatedTimeSeriesRequestAcceptedEventMapper.ToJson(_aggregatedTimeSeriesRequestAcceptedResponse.ToByteArray());
    }
}
