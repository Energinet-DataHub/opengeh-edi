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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.OutgoingMessages.Common;
using DocumentValidation;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.SeedWork;
using Domain.Transactions.Aggregations;
using Infrastructure.Configuration.Serialization;
using Infrastructure.OutgoingMessages.Common;
using Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;
using NodaTime.Text;
using Tests.Factories;
using Tests.Fixtures;
using Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;
using Period = Domain.Transactions.Aggregations.Period;
using Point = Domain.OutgoingMessages.NotifyAggregatedMeasureData.Point;

namespace Tests.Infrastructure.OutgoingMessages.AggregationResult;

public class AggregationResultDocumentWriterTests : IClassFixture<DocumentValidationFixture>
{
    private const string NamespacePrefix = "cim";
    private readonly DocumentValidationFixture _documentValidation;
    private readonly IMessageWriter _messageWriter;
    private readonly IMessageRecordParser _parser;
    private readonly TimeSeriesBuilder _timeSeries;

    public AggregationResultDocumentWriterTests(DocumentValidationFixture documentValidation)
    {
        _documentValidation = documentValidation;
        _parser = new MessageRecordParser(new Serializer());
        _messageWriter = new NotifyAggregatedMeasureDataXmlDocumentWriter(_parser);
        _timeSeries = TimeSeriesBuilder
            .AggregationResult();
    }

    [Fact]
    public async Task Can_create_document()
    {
        var document = await CreateDocument(_timeSeries
            .WithMessageId(SampleData.MessageId)
            .WithTimestamp(SampleData.Timestamp)
            .WithSender(SampleData.SenderId, SampleData.SenderRole)
            .WithReceiver(SampleData.ReceiverId, SampleData.ReceiverRole)
            .WithTransactionId(SampleData.TransactionId)
            .WithGridArea(SampleData.GridAreaCode)
            .WithBalanceResponsibleNumber(SampleData.BalanceResponsibleNumber)
            .WithEnergySupplierNumber(SampleData.EnergySupplierNumber)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .WithPoint(new Point(1, 1m, Quality.Calculated.Name, "2022-12-12T23:00:00Z"))).ConfigureAwait(false);

        await AssertXmlDocument
            .Document(document, NamespacePrefix, _documentValidation.Validator)
            .HasValue("type", "E31")
            .HasValue("mRID", SampleData.MessageId)
            .HasValue("sender_MarketParticipant.mRID", SampleData.SenderId)
            .HasAttributeValue("sender_MarketParticipant.mRID", "codingScheme", "A10")
            .HasValue("sender_MarketParticipant.marketRole.type", CimCode.Of(EnumerationType.FromName<MarketRole>(SampleData.SenderRole.Name)))
            .HasValue("receiver_MarketParticipant.mRID", SampleData.ReceiverId)
            .HasAttributeValue("receiver_MarketParticipant.mRID", "codingScheme", "A10")
            .HasValue("receiver_MarketParticipant.marketRole.type", CimCode.Of(EnumerationType.FromName<MarketRole>(SampleData.ReceiverRole.Name)))
            .HasValue("createdDateTime", SampleData.Timestamp)
            .HasValue("Series[1]/mRID", SampleData.TransactionId.ToString())
            .HasValue("Series[1]/meteringGridArea_Domain.mRID", SampleData.GridAreaCode)
            .HasValue("Series[1]/balanceResponsibleParty_MarketParticipant.mRID", SampleData.BalanceResponsibleNumber)
            .HasValue("Series[1]/energySupplier_MarketParticipant.mRID", SampleData.EnergySupplierNumber)
            .HasAttributeValue("Series[1]/energySupplier_MarketParticipant.mRID", "codingScheme", "A10")
            .HasValue("Series[1]/product", "8716867000030")
            .HasValue("Series[1]/Period/timeInterval/start",  InstantPattern.General.Parse(SampleData.StartOfPeriod).Value.ToString("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture))
            .HasValue("Series[1]/Period/timeInterval/end", InstantPattern.General.Parse(SampleData.EndOfPeriod).Value.ToString("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture))
            .HasValue("Series[1]/Period/Point[1]/position", "1")
            .HasValue("Series[1]/Period/Point[1]/quantity", "1")
            .HasValue("Series[1]/Period/Point[1]/quality",  Quality.Calculated.Code)
            // .HasValue("Series[1]/Period/Point[2]/quality", Quality.From(timeSeries[0].Point[1].Quality).Code)
            // .IsNotPresent("Series[1]/Period/Point[3]/quality")
            .HasValidStructureAsync(DocumentType.AggregationResult).ConfigureAwait(false);
    }

    [Fact]
    public async Task Point_quantity_element_is_excluded_if_no_value()
    {
        _timeSeries
            .WithPoint(new Point(1, null, Quality.Missing.Name, "2022-12-12T23:00:00Z"));

        var document = await CreateDocument(_timeSeries).ConfigureAwait(false);

        AssertXmlDocument
            .Document(document, NamespacePrefix, _documentValidation.Validator)
            .IsNotPresent("Series[1]/Period/Point[1]/quantity");
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
                MeteringPointType.Production.Name,
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
            .Document(message, NamespacePrefix, _documentValidation.Validator)
            .IsNotPresent("Series[1]/marketEvaluationPoint.settlementMethod")
            .IsNotPresent("Series[1]/energySupplier_MarketParticipant.mRID")
            .IsNotPresent("Series[1]/balanceResponsibleParty_MarketParticipant.mRID")
            .HasValidStructureAsync(DocumentType.AggregationResult).ConfigureAwait(false);
    }

    [Theory]
    [InlineData(nameof(SettlementType.Flex), "D01")]
    [InlineData(nameof(SettlementType.NonProfiled), "E02")]
    public async Task Settlement_method_is_translated(string settlementType, string expectedCode)
    {
        _timeSeries
            .WithSettlementMethod(SettlementType.From(settlementType));

        var document = await CreateDocument(_timeSeries).ConfigureAwait(false);

        await AssertXmlDocument
            .Document(document, NamespacePrefix, _documentValidation.Validator)
            .HasValue("Series[1]/marketEvaluationPoint.settlementMethod", expectedCode)
            .HasValidStructureAsync(DocumentType.AggregationResult)
            .ConfigureAwait(false);
    }

    [Theory]
    [InlineData(nameof(ProcessType.BalanceFixing), "D04")]
    public async Task ProcessType_is_translated(string processType, string expectedCode)
    {
        _timeSeries
            .WithProcessType(ProcessType.From(processType));

        var document = await CreateDocument(_timeSeries).ConfigureAwait(false);

        await AssertXmlDocument
            .Document(document, NamespacePrefix, _documentValidation.Validator)
            .HasValue("process.processType", expectedCode)
            .HasValidStructureAsync(DocumentType.AggregationResult)
            .ConfigureAwait(false);
    }

    [Theory]
    [InlineData(nameof(MeteringPointType.Consumption), "E17")]
    [InlineData(nameof(MeteringPointType.Production), "E18")]
    public async Task MeteringPointType_is_translated(string meteringPointType, string expectedCode)
    {
        _timeSeries
            .WithMeteringPointType(EnumerationType.FromName<MeteringPointType>(meteringPointType));

        var document = await CreateDocument(_timeSeries).ConfigureAwait(false);

        await AssertXmlDocument
            .Document(document, NamespacePrefix, _documentValidation.Validator)
            .HasValue("Series[1]/marketEvaluationPoint.type", expectedCode)
            .HasValidStructureAsync(DocumentType.AggregationResult)
            .ConfigureAwait(false);
    }

    [Theory]
    [InlineData(nameof(MeasurementUnit.Kwh), "KWH")]
    public async Task MeasurementUnit_is_translated(string measurementUnit, string expectedCode)
    {
        _timeSeries
            .WithMeasurementUnit(EnumerationType.FromName<MeasurementUnit>(measurementUnit));

        var document = await CreateDocument(_timeSeries).ConfigureAwait(false);

        await AssertXmlDocument
            .Document(document, NamespacePrefix, _documentValidation.Validator)
            .HasValue("Series[1]/quantity_Measure_Unit.name", expectedCode)
            .HasValidStructureAsync(DocumentType.AggregationResult)
            .ConfigureAwait(false);
    }

    [Theory]
    [InlineData(nameof(Resolution.Hourly), "PT1H")]
    [InlineData(nameof(Resolution.QuarterHourly), "PT15M")]
    public async Task Resolution_is_translated(string resolution, string expectedCode)
    {
        _timeSeries
            .WithResolution(EnumerationType.FromName<Resolution>(resolution));

        var document = await CreateDocument(_timeSeries).ConfigureAwait(false);

        await AssertXmlDocument
            .Document(document, NamespacePrefix, _documentValidation.Validator)
            .HasValue("Series[1]/Period/resolution", expectedCode)
            .HasValidStructureAsync(DocumentType.AggregationResult)
            .ConfigureAwait(false);
    }

    [Theory]
    [InlineData(nameof(Quality.Missing), "A02")]
    [InlineData(nameof(Quality.Estimated), "A03")]
    [InlineData(nameof(Quality.Incomplete), "A05")]
    [InlineData(nameof(Quality.Calculated), "A06")]
    public async Task Quality_is_translated(string quality, string expectedCode)
    {
        var point = new Point(1, 1111, EnumerationType.FromName<Quality>(quality).Code, "SampleTime");
        _timeSeries
            .WithPoint(point);

        var document = await CreateDocument(_timeSeries).ConfigureAwait(false);

        await AssertXmlDocument
            .Document(document, NamespacePrefix, _documentValidation.Validator)
            .HasValue("Series[1]/Period/Point[1]/quality", expectedCode)
            .HasValidStructureAsync(DocumentType.AggregationResult)
            .ConfigureAwait(false);
    }

    private static MessageHeader CreateHeader()
    {
        return MessageHeaderFactory.Create(ProcessType.BalanceFixing, MarketRole.MeteredDataResponsible);
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

    private Task<Stream> CreateDocument(TimeSeriesBuilder resultBuilder)
    {
        return _messageWriter.WriteAsync(
            resultBuilder.BuildHeader(),
            new[]
            {
                _parser.From(resultBuilder.BuildTimeSeries()),
            });
    }
}
