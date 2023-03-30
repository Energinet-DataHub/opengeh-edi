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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.OutgoingMessages.Common;
using DocumentValidation;
using Domain.OutgoingMessages;
using Domain.Transactions.Aggregations;
using Infrastructure.Configuration.Serialization;
using Infrastructure.OutgoingMessages.AggregationResult;
using Infrastructure.OutgoingMessages.Common;
using Json.Schema;
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
        _messageWriter = new AggregationResultXmlDocumentWriter(_parser);
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

    [Fact]
    public async Task Point_quantity_element_is_excluded_if_no_value()
    {
        _timeSeries
            .WithPoint(new Point(1, null, Quality.Missing.Name, "2022-12-12T23:00:00Z"));

        var document = await CreateDocument(_timeSeries, DocumentFormat.Xml).ConfigureAwait(false);

        AssertDocument(document, DocumentFormat.Xml)
            .QuantityIsNotPresentForPosition(1);
    }

    [Fact]
    public async Task Quality_element_is_excluded_if_value_is_measured()
    {
        _timeSeries
            .WithPoint(new Point(1, 1, Quality.Measured.Name, "2022-12-12T23:00:00Z"));

        var document = await CreateDocument(_timeSeries, DocumentFormat.Xml).ConfigureAwait(false);

        AssertDocument(document, DocumentFormat.Xml)
            .QualityIsNotPresentForPosition(1);
    }

    [Fact]
    public async Task Settlement_method_is_excluded()
    {
        _timeSeries
            .WithMeteringPointType(MeteringPointType.Production)
            .WithSettlementMethod(null);

        var document = await CreateDocument(_timeSeries, DocumentFormat.Xml).ConfigureAwait(false);

        AssertDocument(document, DocumentFormat.Xml)
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
        if (documentFormat == DocumentFormat.Xml)
        {
            return _messageWriter.WriteAsync(
                resultBuilder.BuildHeader(),
                new[] { _parser.From(resultBuilder.BuildTimeSeries()), });
        }
        else
        {
            var jsonDocumentWriter = new AggregationResultJsonDocumentWriter(_parser);
            return jsonDocumentWriter.WriteAsync(
                resultBuilder.BuildHeader(),
                new[] { _parser.From(resultBuilder.BuildTimeSeries()), });
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

#pragma warning disable
internal sealed class AssertAggregationResultJsonDocument : IAssertAggregationResultDocument
{
    private readonly JsonSchemaProvider _schemas = new(new CimJsonSchemas());
    private readonly JsonDocument _document;
    private readonly JsonElement _root;

    public AssertAggregationResultJsonDocument(Stream document)
    {
        _document = JsonDocument.Parse(document);
        _root = _document.RootElement.GetProperty("NotifyAggregatedMeasureData_MarketDocument");
    }

    public IAssertAggregationResultDocument HasMessageId(string expectedMessageId)
    {
        Assert.Equal(expectedMessageId, _root.GetProperty("mRID").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasSenderId(string expectedSenderId)
    {
        Assert.Equal(expectedSenderId, _root.GetProperty("sender_MarketParticipant.mRID").GetProperty("value").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasReceiverId(string expectedReceiverId)
    {
        Assert.Equal(expectedReceiverId, _root.GetProperty("receiver_MarketParticipant.mRID").GetProperty("value").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasTimestamp(string expectedTimestamp)
    {
        Assert.Equal(expectedTimestamp, _root.GetProperty("createdDateTime").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasTransactionId(Guid expectedTransactionId)
    {
        Assert.Equal(expectedTransactionId, _root.GetProperty("Series").EnumerateArray().ToList()[0].GetProperty("mRID").GetGuid());
        return this;
    }

    public IAssertAggregationResultDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        Assert.Equal(expectedGridAreaCode, _root.GetProperty("Series").EnumerateArray().ToList()[0]
            .GetProperty("meteringGridArea_Domain.mRID")
            .GetProperty("value")
            .ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber)
    {
        Assert.Equal(expectedBalanceResponsibleNumber, _root.GetProperty("Series").EnumerateArray().ToList()[0]
            .GetProperty("balanceResponsibleParty_MarketParticipant.mRID")
            .GetProperty("value").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber)
    {
        Assert.Equal(expectedEnergySupplierNumber, _root.GetProperty("Series").EnumerateArray().ToList()[0]
            .GetProperty("energySupplier_MarketParticipant.mRID")
            .GetProperty("value").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasProductCode(string expectedProductCode)
    {
        Assert.Equal(expectedProductCode, _root.GetProperty("Series").EnumerateArray().ToList()[0].GetProperty("product").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasPeriod(string expectedStartOfPeriod, string expectedEndOfPeriod)
    {
        Assert.Equal(expectedStartOfPeriod, _root.GetProperty("Series").EnumerateArray().ToList()[0]
            .GetProperty("Period")
            .GetProperty("timeInterval")
            .GetProperty("start")
            .GetProperty("value").ToString());
        Assert.Equal(expectedEndOfPeriod, _root.GetProperty("Series").EnumerateArray().ToList()[0]
            .GetProperty("Period")
            .GetProperty("timeInterval")
            .GetProperty("end")
            .GetProperty("value").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasPoint(int position, int quantity)
    {
        var point = _root.GetProperty("Series").EnumerateArray().ToList()[0]
            .GetProperty("Period")
            .GetProperty("Point").EnumerateArray().ToList()[position - 1];

        Assert.NotNull(point);
        Assert.Equal(position, point.GetProperty("position").GetProperty("value").GetInt32());
        Assert.Equal(quantity, point.GetProperty("quantity").GetInt32());
        return this;
    }

    public async Task<IAssertAggregationResultDocument> DocumentIsValidAsync()
    {
        var schema = await _schemas.GetSchemaAsync<JsonSchema>("NOTIFYAGGREGATEDMEASUREDATA", "0").ConfigureAwait(false);
        var validationOptions = new EvaluationOptions()
        {
            OutputFormat = OutputFormat.List,
        };
        var validationResult = schema.Evaluate(_document, validationOptions);
        var errors = validationResult.Details.Where(detail => detail.HasErrors).Select(x => x.Errors).ToList()
            .SelectMany(e => e.Values).ToList();
        Assert.True(validationResult.IsValid, string.Join("\n", errors));
        return this;
    }

    public IAssertAggregationResultDocument SettlementMethodIsNotPresent()
    {
        throw new NotImplementedException();
    }

    public IAssertAggregationResultDocument EnergySupplierNumberIsNotPresent()
    {
        throw new NotImplementedException();
    }

    public IAssertAggregationResultDocument BalanceResponsibleNumberIsNotPresent()
    {
        throw new NotImplementedException();
    }

    public IAssertAggregationResultDocument QuantityIsNotPresentForPosition(int position)
    {
        throw new NotImplementedException();
    }

    public IAssertAggregationResultDocument QualityIsNotPresentForPosition(int position)
    {
        throw new NotImplementedException();
    }
}
