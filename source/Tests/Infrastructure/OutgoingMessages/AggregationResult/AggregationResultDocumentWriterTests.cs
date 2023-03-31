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

using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Application.OutgoingMessages.Common;
using Domain.OutgoingMessages;
using Domain.Transactions.Aggregations;
using Infrastructure.Configuration.Serialization;
using Infrastructure.OutgoingMessages.AggregationResult;
using Infrastructure.OutgoingMessages.Common;
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
    private readonly IMessageRecordParser _parser;
    private readonly TimeSeriesBuilder _timeSeries;

    public AggregationResultDocumentWriterTests(DocumentValidationFixture documentValidation)
    {
        _documentValidation = documentValidation;
        _parser = new MessageRecordParser(new Serializer());
        _timeSeries = TimeSeriesBuilder
            .AggregationResult();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
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
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Point_quantity_element_is_excluded_if_no_value(string documentFormat)
    {
        _timeSeries
            .WithPoint(new Point(1, null, Quality.Missing.Name, "2022-12-12T23:00:00Z"));

        var document = await CreateDocument(_timeSeries, DocumentFormat.From(documentFormat)).ConfigureAwait(false);

        AssertDocument(document, DocumentFormat.From(documentFormat))
            .QuantityIsNotPresentForPosition(1);
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Quality_element_is_excluded_if_value_is_measured(string documentFormat)
    {
        _timeSeries
            .WithPoint(new Point(1, 1, Quality.Measured.Name, "2022-12-12T23:00:00Z"));

        var document = await CreateDocument(_timeSeries, DocumentFormat.From(documentFormat)).ConfigureAwait(false);

        AssertDocument(document, DocumentFormat.From(documentFormat))
            .QualityIsNotPresentForPosition(1);
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Settlement_method_is_excluded(string documentFormat)
    {
        _timeSeries
            .WithMeteringPointType(MeteringPointType.Production)
            .WithSettlementMethod(null);

        var document = await CreateDocument(_timeSeries, DocumentFormat.From(documentFormat)).ConfigureAwait(false);

        AssertDocument(document, DocumentFormat.From(documentFormat))
            .SettlementMethodIsNotPresent();
    }

    [Fact]
    public async Task Energy_supplier_number_is_excluded()
    {
        _timeSeries
            .WithEnergySupplierNumber(null);

        var document = await CreateDocument(_timeSeries, DocumentFormat.Xml).ConfigureAwait(false);

        AssertDocument(document, DocumentFormat.Xml)
            .EnergySupplierNumberIsNotPresent();
    }

    [Fact]
    public async Task Balance_responsible_number_is_excluded()
    {
        _timeSeries
            .WithBalanceResponsibleNumber(null);

        var document = await CreateDocument(_timeSeries, DocumentFormat.Xml).ConfigureAwait(false);

        AssertDocument(document, DocumentFormat.Xml)
            .BalanceResponsibleNumberIsNotPresent();
    }

    private Task<Stream> CreateDocument(TimeSeriesBuilder resultBuilder, DocumentFormat documentFormat)
    {
        var documentHeader = resultBuilder.BuildHeader();
        var records = _parser.From(resultBuilder.BuildTimeSeries());
        if (documentFormat == DocumentFormat.Xml)
        {
            return new AggregationResultXmlDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records, });
        }
        else
        {
            return new AggregationResultJsonDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records, });
        }
    }

    private IAssertAggregationResultDocument AssertDocument(Stream document, DocumentFormat documentFormat)
    {
        if (documentFormat == DocumentFormat.Xml)
        {
            var assertXmlDocument = AssertXmlDocument.Document(document, "cim", _documentValidation.Validator);
            return new AssertAggregationResultXmlDocument(assertXmlDocument);
        }
        else
        {
            return new AssertAggregationResultJsonDocument(document);
        }
    }
}
