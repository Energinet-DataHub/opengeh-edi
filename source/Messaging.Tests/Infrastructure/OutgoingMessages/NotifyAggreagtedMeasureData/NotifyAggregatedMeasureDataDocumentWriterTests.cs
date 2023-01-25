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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Schema;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Messaging.Domain.Transactions.Aggregations;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.IncomingMessages.SchemaStore;
using Messaging.Infrastructure.OutgoingMessages.Common;
using Messaging.Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;
using Messaging.Tests.Factories;
using Messaging.Tests.Infrastructure.OutgoingMessages.AccountingPointCharacteristics;
using Messaging.Tests.Infrastructure.OutgoingMessages.Asserts;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;
using Xunit;
using Period = Messaging.Domain.Transactions.Aggregations.Period;

namespace Messaging.Tests.Infrastructure.OutgoingMessages.NotifyAggreagtedMeasureData;

public class NotifyAggregatedMeasureDataDocumentWriterTests
{
    private const string NamespacePrefix = "cim";
    private readonly IMessageWriter _messageWriter;
    private readonly ISchemaProvider _schemaProvider;
    private readonly IMessageRecordParser _parser;

    public NotifyAggregatedMeasureDataDocumentWriterTests()
    {
        _parser = new MessageRecordParser(new Serializer());
        _schemaProvider = new XmlSchemaProvider();
        _messageWriter = new NotifyAggregatedMeasureDataMessageWriter(_parser);
    }

    [Fact]
    public async Task Can_create_document()
    {
        var header = new MessageHeader(
            ProcessType.BalanceFixing.Code,
            "1234567890123",
            MarketRole.MeteringPointAdministrator.Name,
            "1234567890321",
            MarketRole.GridOperator.Name,
            Guid.NewGuid().ToString(),
            SystemClock.Instance.GetCurrentInstant());

        var timeSeries = new List<TimeSeries>()
        {
            new(
                Guid.NewGuid(),
                "870",
                "E18",
                "KWH",
                "PT1H",
                new Period(InstantPattern.General.Parse("2022-02-12T23:00:00Z").Value, InstantPattern.General.Parse("2022-02-13T23:00:00Z").Value),
                new List<Point>()
                {
                    new(1, 11, "A05", "2022-02-12T23:00Z"),
                    new(2, null, null, "2022-02-13T23:00Z"),
                }),
        };

        var message = await _messageWriter.WriteAsync(header, timeSeries.Select(record => _parser.From(record)).ToList()).ConfigureAwait(false);

        await AssertXmlDocument
            .Document(message, NamespacePrefix)
            .HasValue("type", "E31")
            .HasValue("mRID", header.MessageId)
            .HasValue("process.processType", header.ProcessType)
            .HasValue("sender_MarketParticipant.mRID", header.SenderId)
            .HasAttributeValue("sender_MarketParticipant.mRID", "codingScheme", "A10")
            .HasValue("sender_MarketParticipant.marketRole.type", header.SenderRole)
            .HasValue("receiver_MarketParticipant.mRID", header.ReceiverId)
            .HasAttributeValue("receiver_MarketParticipant.mRID", "codingScheme", "A10")
            .HasValue("receiver_MarketParticipant.marketRole.type", header.ReceiverRole)
            .HasValue("createdDateTime", header.TimeStamp.ToString())
            .HasValue("Series[1]/mRID", timeSeries[0].TransactionId.ToString())
            .HasValue("Series[1]/meteringGridArea_Domain.mRID", timeSeries[0].GridAreaCode)
            .HasValue("Series[1]/marketEvaluationPoint.type", timeSeries[0].MeteringPointType)
            .HasValue("Series[1]/quantity_Measure_Unit.name", timeSeries[0].MeasureUnitType)
            .HasValue("Series[1]/Period/resolution", timeSeries[0].Resolution)
            .HasValue("Series[1]/Period/timeInterval/start", "2022-02-12T23:00Z")
            .HasValue("Series[1]/Period/timeInterval/end", "2022-02-13T23:00Z")
            .HasValue("Series[1]/Period/Point[1]/position", timeSeries[0].Point[0].Position.ToString(NumberFormatInfo.InvariantInfo))
            .HasValue("Series[1]/Period/Point[1]/quantity", timeSeries[0].Point[0].Quantity.ToString()!)
            .HasValue("Series[1]/Period/Point[1]/quality", timeSeries[0].Point[0].Quality!)
            .HasValue("Series[1]/Period/Point[2]/position", timeSeries[0].Point[1].Position.ToString(NumberFormatInfo.InvariantInfo))
            .IsNotPresent("Series[1]/Period/Point[2]/quantity")
            .IsNotPresent("Series[1]/Period/Point[2]/quality")
            .HasValidStructureAsync((await GetSchema().ConfigureAwait(false))!).ConfigureAwait(false);
    }

    private Task<XmlSchema?> GetSchema()
    {
        return _schemaProvider.GetSchemaAsync<XmlSchema>("notifyaggregatedmeasuredata", "0.1");
    }
}
