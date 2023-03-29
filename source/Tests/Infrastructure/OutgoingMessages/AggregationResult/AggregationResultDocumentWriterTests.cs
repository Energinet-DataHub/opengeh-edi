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
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Application.OutgoingMessages.Common;
using DocumentValidation;
using Domain.OutgoingMessages;
using Domain.Transactions.Aggregations;
using Infrastructure.Configuration.Serialization;
using Infrastructure.OutgoingMessages.Common;
using Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;
using NodaTime.Text;
using Tests.Factories;
using Tests.Fixtures;
using Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;
using DocumentFormat = Domain.OutgoingMessages.DocumentFormat;
using Point = Domain.OutgoingMessages.NotifyAggregatedMeasureData.Point;

namespace Tests.Infrastructure.OutgoingMessages.AggregationResult;

public class AggregationResultDocumentWriterTests : IClassFixture<DocumentValidationFixture>
{
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

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    public async Task Can_create_document(string documentFormat)
    {
        var document = await CreateDocument(
                _timeSeries
            .WithMessageId(SampleData.MessageId)
            .WithTimestamp(SampleData.Timestamp)
            .WithSender(SampleData.SenderId, SampleData.SenderRole)
            .WithReceiver(SampleData.ReceiverId, SampleData.ReceiverRole)
            .WithTransactionId(SampleData.TransactionId)
            .WithGridArea(SampleData.GridAreaCode)
            .WithBalanceResponsibleNumber(SampleData.BalanceResponsibleNumber)
            .WithEnergySupplierNumber(SampleData.EnergySupplierNumber)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .WithPoint(new Point(1, 1m, Quality.Calculated.Name, "2022-12-12T23:00:00Z")),
                DocumentFormat.From(documentFormat))
            .ConfigureAwait(false);

        await AssertDocument(document, DocumentFormat.From(documentFormat))
            .HasMessageId(SampleData.MessageId)
            .HasSenderId(SampleData.SenderId)
            .HasReceiverId(SampleData.ReceiverId)
            .HasTimestamp(SampleData.Timestamp)
            .HasTransactionId(SampleData.TransactionId)
            .HasGridAreaCode(SampleData.GridAreaCode)
            .HasBalanceResponsibleNumber(SampleData.BalanceResponsibleNumber)
            .HasEnergySupplierNumber(SampleData.EnergySupplierNumber)
            .HasProductCode("8716867000030")
            .HasPeriod(
                InstantPattern.General.Parse(SampleData.StartOfPeriod).Value
                    .ToString("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture),
                InstantPattern.General.Parse(SampleData.EndOfPeriod).Value
                    .ToString("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture))
            .HasPoint(1, 1)
            .DocumentIsValidAsync().ConfigureAwait(false);
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml), "E31")]
    public async Task Type_is_translated(string documentFormat, string expectedType)
    {
        var document = await CreateDocument(_timeSeries, DocumentFormat.From(documentFormat)).ConfigureAwait(false);

        new AssertAggregationResultXmlDocument(AssertXmlDocument.Document(document, "cim", _documentValidation.Validator))
            .HasType(expectedType);
    }

    [Fact]
    public async Task Point_quantity_element_is_excluded_if_no_value()
    {
        _timeSeries
            .WithPoint(new Point(1, null, Quality.Missing.Name, "2022-12-12T23:00:00Z"));

        var document = await CreateDocument(_timeSeries).ConfigureAwait(false);

        AssertDocument(document, DocumentFormat.Xml)
            .QuantityIsNotPresentForPosition(1);
    }

    [Fact]
    public async Task Quality_element_is_excluded_if_value_is_measured()
    {
        _timeSeries
            .WithPoint(new Point(1, 1, Quality.Measured.Name, "2022-12-12T23:00:00Z"));

        var document = await CreateDocument(_timeSeries).ConfigureAwait(false);

        AssertDocument(document, DocumentFormat.Xml)
            .QualityIsNotPresentForPosition(1);
    }

    [Fact]
    public async Task Settlement_method_is_excluded()
    {
        _timeSeries
            .WithMeteringPointType(MeteringPointType.Production)
            .WithSettlementMethod(null);

        var document = await CreateDocument(_timeSeries).ConfigureAwait(false);

        AssertDocument(document, DocumentFormat.Xml)
            .SettlementMethodIsNotPresent();
    }

    [Fact]
    public async Task Energy_supplier_number_is_excluded()
    {
        _timeSeries
            .WithEnergySupplierNumber(null);

        var document = await CreateDocument(_timeSeries).ConfigureAwait(false);

        AssertDocument(document, DocumentFormat.Xml)
            .EnergySupplierNumberIsNotPresent();
    }

    [Fact]
    public async Task Balance_responsible_number_is_excluded()
    {
        _timeSeries
            .WithBalanceResponsibleNumber(null);

        var document = await CreateDocument(_timeSeries).ConfigureAwait(false);

        AssertDocument(document, DocumentFormat.Xml)
            .BalanceResponsibleNumberIsNotPresent();
    }

    private Task<Stream> CreateDocument(TimeSeriesBuilder resultBuilder, DocumentFormat? documentFormat = null)
    {
        return _messageWriter.WriteAsync(
            resultBuilder.BuildHeader(),
            new[]
            {
                _parser.From(resultBuilder.BuildTimeSeries()),
            });
    }

    private AssertAggregationResultXmlDocument AssertDocument(Stream document, DocumentFormat documentFormat)
    {
        var assertXmlDocument = AssertXmlDocument.Document(document, "cim", _documentValidation.Validator);
        return new AssertAggregationResultXmlDocument(assertXmlDocument);
    }
}

#pragma warning disable
public class AssertAggregationResultXmlDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertAggregationResultXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
    }

    public AssertAggregationResultXmlDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("mRID", expectedMessageId);
        return this;
    }

    public AssertAggregationResultXmlDocument HasSenderId(string expectedSenderId)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.mRID", expectedSenderId);
        return this;
    }

    public AssertAggregationResultXmlDocument HasReceiverId(string expectedReceiverId)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", expectedReceiverId);
        return this;
    }

    public AssertAggregationResultXmlDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp);
        return this;
    }

    public AssertAggregationResultXmlDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue("Series[1]/mRID", expectedTransactionId.ToString());
        return this;
    }

    public AssertAggregationResultXmlDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        _documentAsserter.HasValue("Series[1]/meteringGridArea_Domain.mRID", expectedGridAreaCode);
        return this;
    }

    public AssertAggregationResultXmlDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber)
    {
        _documentAsserter.HasValue("Series[1]/balanceResponsibleParty_MarketParticipant.mRID",
            expectedBalanceResponsibleNumber);
        return this;
    }

    public AssertAggregationResultXmlDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber)
    {
        _documentAsserter.HasValue("Series[1]/energySupplier_MarketParticipant.mRID", expectedEnergySupplierNumber);
        return this;
    }

    public AssertAggregationResultXmlDocument HasProductCode(string expectedProductCode)
    {
        _documentAsserter.HasValue("Series[1]/product", expectedProductCode);
        return this;
    }

    public AssertAggregationResultXmlDocument HasPeriod(string expectedStartOfPeriod, string expectedEndOfPeriod)
    {
        _documentAsserter
            .HasValue("Series[1]/Period/timeInterval/start", expectedStartOfPeriod)
            .HasValue("Series[1]/Period/timeInterval/end", expectedEndOfPeriod);
        return this;
    }

    public AssertAggregationResultXmlDocument HasPoint(int position, int quantity)
    {
        _documentAsserter
            .HasValue("Series[1]/Period/Point[1]/position", position.ToString())
            .HasValue("Series[1]/Period/Point[1]/quantity", quantity.ToString());
        return this;
    }

    public async Task<AssertAggregationResultXmlDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.AggregationResult).ConfigureAwait(false);
        return this;
    }

    public AssertAggregationResultXmlDocument HasType(string expectedType)
    {
        _documentAsserter.HasValue("type", expectedType);
        return this;
    }

    public AssertAggregationResultXmlDocument HasSenderIdCodingScheme(string expectedCodingScheme)
    {
        _documentAsserter.HasAttributeValue("sender_MarketParticipant.mRID", "codingScheme", expectedCodingScheme);
        return this;
    }

    public AssertAggregationResultXmlDocument HasReceiverIdCodingScheme(string expectedCodingScheme)
    {
        _documentAsserter.HasAttributeValue("receiver_MarketParticipant.mRID", "codingScheme", expectedCodingScheme);
        return this;
    }

    public AssertAggregationResultXmlDocument HasEnergySupplierCodingScheme(string expectedCodingScheme)
    {
        _documentAsserter.HasAttributeValue("Series[1]/energySupplier_MarketParticipant.mRID", "codingScheme", expectedCodingScheme);
        return this;
    }

    public AssertAggregationResultXmlDocument HasSenderRole(string expectedCode)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.marketRole.type", expectedCode);
        return this;
    }

    public AssertAggregationResultXmlDocument HasReceiverRole(string expectedCode)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.marketRole.type", expectedCode);
        return this;
    }

    public AssertAggregationResultXmlDocument SettlementMethodIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/marketEvaluationPoint.settlementMethod");
        return this;
    }

    public AssertAggregationResultXmlDocument EnergySupplierNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/energySupplier_MarketParticipant.mRID");
        return this;
    }

    public AssertAggregationResultXmlDocument BalanceResponsibleNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/balanceResponsibleParty_MarketParticipant.mRID");
        return this;
    }

    public AssertAggregationResultXmlDocument QuantityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"Series[1]/Period/Point[{position}]/quantity");
        return this;
    }

    public AssertAggregationResultXmlDocument QualityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"Series[1]/Period/Point[{position}]/quality");
        return this;
    }
}
