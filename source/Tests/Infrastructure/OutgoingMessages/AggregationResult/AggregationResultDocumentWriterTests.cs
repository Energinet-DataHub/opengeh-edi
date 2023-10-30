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

using System.IO;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Application.OutgoingMessages.Common;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Serialization;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.AggregationResult;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common;
using Energinet.DataHub.EDI.Tests.Factories;
using Energinet.DataHub.EDI.Tests.Fixtures;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;
using DocumentFormat = Energinet.DataHub.EDI.Domain.Documents.DocumentFormat;
using Point = Energinet.DataHub.EDI.Domain.OutgoingMessages.NotifyAggregatedMeasureData.Point;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.AggregationResult;

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
    [InlineData(nameof(DocumentFormat.Ebix))]
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
                    .WithPoint(new Point(1, 1m, Quality.Calculated.Name, "2022-12-12T23:00:00Z"))
                    .WithOriginalTransactionIdReference(SampleData.OriginalTransactionIdReference)
                    .WithSettlementMethod(SettlementType.NonProfiled),
                DocumentFormat.From(documentFormat));

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
                new Period(SampleData.StartOfPeriod, SampleData.EndOfPeriod))
            .HasPoint(1, 1)
            .HasOriginalTransactionIdReference(SampleData.OriginalTransactionIdReference)
            .HasSettlementMethod(SettlementType.NonProfiled)
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix))]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Point_quantity_element_is_excluded_if_no_value(string documentFormat)
    {
        _timeSeries
            .WithPoint(new Point(1, null, Quality.Missing.Name, "2022-12-12T23:00:00Z"));

        var document = await CreateDocument(_timeSeries, DocumentFormat.From(documentFormat));

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

        var document = await CreateDocument(_timeSeries, DocumentFormat.From(documentFormat));

        AssertDocument(document, DocumentFormat.From(documentFormat))
            .QualityIsNotPresentForPosition(1);
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix))]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Settlement_method_is_excluded(string documentFormat)
    {
        _timeSeries
            .WithMeteringPointType(MeteringPointType.Production)
            .WithSettlementMethod(null);

        var document = await CreateDocument(_timeSeries, DocumentFormat.From(documentFormat));

        AssertDocument(document, DocumentFormat.From(documentFormat))
            .SettlementMethodIsNotPresent();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix))]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Energy_supplier_number_is_excluded(string documentFormat)
    {
        _timeSeries
            .WithEnergySupplierNumber(null);

        var document = await CreateDocument(_timeSeries, DocumentFormat.From(documentFormat));

        AssertDocument(document, DocumentFormat.From(documentFormat))
            .EnergySupplierNumberIsNotPresent();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix))]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Balance_responsible_number_is_excluded(string documentFormat)
    {
        _timeSeries
            .WithBalanceResponsibleNumber(null);

        var document = await CreateDocument(_timeSeries, DocumentFormat.From(documentFormat));

        AssertDocument(document, DocumentFormat.From(documentFormat))
            .BalanceResponsibleNumberIsNotPresent();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix), nameof(BusinessReason.PreliminaryAggregation))]
    [InlineData(nameof(DocumentFormat.Xml), nameof(BusinessReason.PreliminaryAggregation))]
    [InlineData(nameof(DocumentFormat.Json), nameof(BusinessReason.PreliminaryAggregation))]
    [InlineData(nameof(DocumentFormat.Ebix), nameof(BusinessReason.BalanceFixing))]
    [InlineData(nameof(DocumentFormat.Xml), nameof(BusinessReason.BalanceFixing))]
    [InlineData(nameof(DocumentFormat.Json), nameof(BusinessReason.BalanceFixing))]
    public async Task Business_reason_is_translated(string documentFormat, string processType)
    {
        _timeSeries.WithBusinessReason(BusinessReason.FromName(processType));

        var document = await CreateDocument(_timeSeries, DocumentFormat.From(documentFormat));

        AssertDocument(document, DocumentFormat.From(documentFormat))
            .HasBusinessReason(BusinessReason.FromName(processType))
            .SettlementVersionIsNotPresent();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix), nameof(BusinessReason.Correction), nameof(SettlementVersion.FirstCorrection))]
    [InlineData(nameof(DocumentFormat.Xml), nameof(BusinessReason.Correction), nameof(SettlementVersion.FirstCorrection))]
    [InlineData(nameof(DocumentFormat.Json), nameof(BusinessReason.Correction), nameof(SettlementVersion.FirstCorrection))]
    public async Task Business_reason_and_settlement_version_is_translated(string documentFormat, string processType, string settlementVersion)
    {
        _timeSeries
            .WithMessageId(SampleData.MessageId)
            .WithTransactionId(SampleData.TransactionId)
            .WithBusinessReason(BusinessReason.FromName(processType))
            .WithSettlementVersion(SettlementVersion.FromName(settlementVersion))
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .WithPoint(new Point(1, 1m, Quality.Calculated.Name, "2022-12-12T23:00:00Z"));

        var document = await CreateDocument(_timeSeries, DocumentFormat.From(documentFormat));

        await AssertDocument(document, DocumentFormat.From(documentFormat))
            .HasBusinessReason(BusinessReason.FromName(processType))
            .HasSettlementVersion(SettlementVersion.FromName(settlementVersion))
            .DocumentIsValidAsync();
    }

    private Task<Stream> CreateDocument(TimeSeriesBuilder resultBuilder, DocumentFormat documentFormat)
    {
        var documentHeader = resultBuilder.BuildHeader();
        var records = _parser.From(resultBuilder.BuildTimeSeries());
        if (documentFormat == DocumentFormat.Ebix)
        {
            return new AggregationResultEbixDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records, });
        }
        else if (documentFormat == DocumentFormat.Xml)
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
        if (documentFormat == DocumentFormat.Ebix)
        {
            var assertEbixDocument = AssertEbixDocument.Document(document, "ns0", _documentValidation.Validator);
            return new AssertAggregationResultEbixDocument(assertEbixDocument);
        }
        else if (documentFormat == DocumentFormat.Xml)
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
