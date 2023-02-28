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
using Application.OutgoingMessages.Common;
using DocumentValidation;
using DocumentValidation.Validators;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.SeedWork;
using Domain.Transactions.Aggregations;
using Infrastructure.Configuration.Serialization;
using Infrastructure.OutgoingMessages.Common;
using Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;
using NodaTime;
using NodaTime.Text;
using Tests.Domain.Transactions.MoveIn;
using Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;
using Period = Domain.Transactions.Aggregations.Period;

namespace Tests.Infrastructure.OutgoingMessages.NotifyAggreagtedMeasureData;

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
        var header = CreateHeader();
        var timeSeries = CreateSeriesFor(MeteringPointType.Consumption);

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
            .HasValue("Series[1]/balanceResponsibleParty_MarketParticipant.mRID", timeSeries[0].BalanceResponsibleNumber!)
            .HasValue("Series[1]/energySupplier_MarketParticipant.mRID", timeSeries[0].EnergySupplierNumber!)
            .HasAttributeValue("Series[1]/energySupplier_MarketParticipant.mRID", "codingScheme", "A10")
            .HasValue("Series[1]/marketEvaluationPoint.type",  EnumerationType.FromName<MeteringPointType>(timeSeries[0].MeteringPointType).Code)
            .HasValue("Series[1]/marketEvaluationPoint.settlementMethod", timeSeries[0].SettlementType!)
            .HasValue("Series[1]/product", "8716867000030")
            .HasValue("Series[1]/quantity_Measure_Unit.name", timeSeries[0].MeasureUnitType)
            .HasValue("Series[1]/Period/resolution", timeSeries[0].Resolution)
            .HasValue("Series[1]/Period/timeInterval/start", "2022-02-12T23:00Z")
            .HasValue("Series[1]/Period/timeInterval/end", "2022-02-13T23:00Z")
            .HasValue("Series[1]/Period/Point[1]/position", timeSeries[0].Point[0].Position.ToString(NumberFormatInfo.InvariantInfo))
            .HasValue("Series[1]/Period/Point[1]/quantity", timeSeries[0].Point[0].Quantity.ToString()!)
            .HasValue("Series[1]/Period/Point[1]/quality",  Quality.From(timeSeries[0].Point[0].Quality).Code)
            .HasValue("Series[1]/Period/Point[2]/position", timeSeries[0].Point[1].Position.ToString(NumberFormatInfo.InvariantInfo))
            .IsNotPresent("Series[1]/Period/Point[2]/quantity")
            .HasValue("Series[1]/Period/Point[2]/quality", Quality.From(timeSeries[0].Point[1].Quality).Code)
            .IsNotPresent("Series[1]/Period/Point[3]/quality")
            .HasValidStructureAsync((await GetSchema().ConfigureAwait(false))!).ConfigureAwait(false);
        var validationResult = await new DocumentValidator(new[] { new CimXmlValidator((XmlSchemaProvider)_schemaProvider) }).ValidateAsync(message, DocumentFormat.CimXml, DocumentType.AggregationResult).ConfigureAwait(false);
        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public async Task Exclude_optional_attributes_if_value_is_unspecified()
    {
        var header = CreateHeader();
        var timeSeries = new List<TimeSeries>()
        {
            new(
                Guid.NewGuid(),
                "870",
                MeteringPointType.Production.Code,
                null,
                "KWH",
                "PT1H",
                null,
                null,
                new Period(
                    InstantPattern.General.Parse("2022-02-12T23:00:00Z").Value,
                    InstantPattern.General.Parse("2022-02-13T23:00:00Z").Value),
                new List<Point> { }),
        };

        var message = await _messageWriter.WriteAsync(header, timeSeries.Select(record => _parser.From(record)).ToList()).ConfigureAwait(false);

        await AssertXmlDocument
            .Document(message, NamespacePrefix)
            .IsNotPresent("Series[1]/marketEvaluationPoint.settlementMethod")
            .IsNotPresent("Series[1]/energySupplier_MarketParticipant.mRID")
            .IsNotPresent("Series[1]/balanceResponsibleParty_MarketParticipant.mRID")
            .HasValidStructureAsync((await GetSchema().ConfigureAwait(false))!).ConfigureAwait(false);
    }

    private static MessageHeader CreateHeader()
    {
        return new MessageHeader(
            ProcessType.BalanceFixing.Code,
            "1234567890123",
            MarketRole.MeteredDataResponsible.Name,
            "1234567890321",
            MarketRole.GridOperator.Name,
            Guid.NewGuid().ToString(),
            SystemClock.Instance.GetCurrentInstant());
    }

    private static List<TimeSeries> CreateSeriesFor(MeteringPointType meteringPointType)
    {
        var timeSeries = new List<TimeSeries>()
        {
            new(
                Guid.NewGuid(),
                "870",
                meteringPointType.Name,
                meteringPointType == MeteringPointType.Consumption ? SettlementType.NonProfiled.Code : null,
                "KWH",
                "PT1H",
                SampleData.EnergySupplierNumber,
                SampleData.BalanceResponsibleNumber,
                new Period(
                    InstantPattern.General.Parse("2022-02-12T23:00:00Z").Value,
                    InstantPattern.General.Parse("2022-02-13T23:00:00Z").Value),
                new List<Point>()
                {
                    new(1, 11, Quality.Incomplete.Name, "2022-02-12T23:00Z"),
                    new(2, null, Quality.Missing.Name, "2022-02-13T23:00Z"),
                    new(2, null, Quality.Measured.Name, "2022-02-13T23:00Z"),
                }),
        };
        return timeSeries;
    }

    private Task<XmlSchema?> GetSchema()
    {
        return _schemaProvider.GetSchemaAsync<XmlSchema>("notifyaggregatedmeasuredata", "0.1");
    }
}
