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
using System.Threading.Tasks;
using System.Xml.Schema;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.SchemaStore;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Messaging.Infrastructure.Common;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;
using Messaging.Tests.Infrastructure.OutgoingMessages.Asserts;
using NodaTime;
using Xunit;
using Period = Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData.Period;

namespace Messaging.Tests.Infrastructure.OutgoingMessages.NotifyAggreagtedMeasureData;

public class NotifyAggregatedMeasureDataDocumentWriterTests
{
    private const string NamespacePrefix = "cim";
    private readonly IDocumentWriter _documentWriter;
    private readonly ISchemaProvider _schemaProvider;
    private readonly IMarketActivityRecordParser _parser;

    public NotifyAggregatedMeasureDataDocumentWriterTests()
    {
        _parser = new MarketActivityRecordParser(new Serializer());
        _schemaProvider = new XmlSchemaProvider();
        _documentWriter = new NotifyAggregatedMeasureDataDocumentWriter(_parser);
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
                "E18",
                "KWH",
                new Period(
                    "PT1H",
                    new TimeInterval(
                        "2022-02-12T23:00Z",
                        "2022-02-12T23:00Z"),
                    new List<Point>()
                    {
                        new(1, 11, "A05"),
                    })),
        };

        var message = await _documentWriter.WriteAsync(header, timeSeries.Select(record => _parser.From(record)).ToList()).ConfigureAwait(false);

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
            .HasValue("Series[1]/mRID", timeSeries[0].Id.ToString())
            .HasValidStructureAsync((await GetSchema().ConfigureAwait(false))!).ConfigureAwait(false);
    }

    private Task<XmlSchema?> GetSchema()
    {
        return _schemaProvider.GetSchemaAsync<XmlSchema>("notifyaggregatedmeasuredata", "0.1");
    }
}
