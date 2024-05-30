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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Tests.Factories;
using Energinet.DataHub.EDI.Tests.Fixtures;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;

public class NotifyAggregatedMeasureDataDocumentWriterTests : IClassFixture<DocumentValidationFixture>
{
    private readonly DocumentValidationFixture _documentValidation;
    private readonly MessageRecordParser _parser;
    private readonly EnergyResultMessageTimeSeriesBuilder _energyResultMessageTimeSeries;

    public NotifyAggregatedMeasureDataDocumentWriterTests(
        DocumentValidationFixture documentValidation)
    {
        _documentValidation = documentValidation;
        _parser = new MessageRecordParser(new Serializer());
        _energyResultMessageTimeSeries = EnergyResultMessageTimeSeriesBuilder
            .AggregationResult();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix))]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Can_create_document(string documentFormat)
    {
        var document = await CreateDocument(
                _energyResultMessageTimeSeries
                    .WithMessageId(SampleData.MessageId)
                    .WithTimestamp(SampleData.Timestamp)
                    .WithSender(SampleData.SenderId, SampleData.SenderRole)
                    .WithReceiver(SampleData.ReceiverId, SampleData.ReceiverRole)
                    .WithTransactionId(SampleData.TransactionId)
                    .WithGridArea(SampleData.GridAreaCode)
                    .WithBalanceResponsibleNumber(SampleData.BalanceResponsibleNumber)
                    .WithEnergySupplierNumber(SampleData.EnergySupplierNumber)
                    .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
                    .WithPoint(new EnergyResultMessagePoint(1, 1m, CalculatedQuantityQuality.Calculated, "2022-12-12T23:00:00Z"))
                    .WithSettlementMethod(SettlementMethod.NonProfiled),
                DocumentFormat.FromName(documentFormat));

        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
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
            .HasSettlementMethod(SettlementMethod.NonProfiled)
            .HasCalculationResultVersion(1)
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix))]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Point_quantity_element_is_excluded_if_no_value(string documentFormat)
    {
        _energyResultMessageTimeSeries
            .WithPoint(new EnergyResultMessagePoint(1, null, CalculatedQuantityQuality.Missing, "2022-12-12T23:00:00Z"));

        var document = await CreateDocument(_energyResultMessageTimeSeries, DocumentFormat.FromName(documentFormat));

        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .QuantityIsNotPresentForPosition(1)
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Quality_element_is_excluded_if_edi_quantity_quality_is_measured(
        string documentFormat)
    {
        _energyResultMessageTimeSeries
            .WithPoint(new EnergyResultMessagePoint(1, 1, CalculatedQuantityQuality.Measured, "2022-12-12T23:00:00Z"));

        var document = await CreateDocument(_energyResultMessageTimeSeries, DocumentFormat.FromName(documentFormat));

        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .QualityIsNotPresentForPosition(1)
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Quality_element_has_correct_code_for_cim_formats(string documentFormat)
    {
        var points = Enum.GetValues(typeof(CalculatedQuantityQuality))
            .Cast<CalculatedQuantityQuality>()
            .Order()
            .Where(x => x != CalculatedQuantityQuality.Measured)
            .Select((quality, index) => new EnergyResultMessagePoint(index + 1, 1, quality, "2022-12-12T23:00:00Z"))
            .ToList();

        points.ForEach(point => _energyResultMessageTimeSeries.WithPoint(point));

        var document = await CreateDocument(_energyResultMessageTimeSeries, DocumentFormat.FromName(documentFormat));

        // Assert
        points.Should().HaveCount(5);
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .QualityIsPresentForPosition(1, CimCode.QuantityQualityCodeIncomplete)
            .QualityIsPresentForPosition(2, CimCode.QuantityQualityCodeIncomplete)
            .QualityIsPresentForPosition(3, CimCode.QuantityQualityCodeEstimated)
            .QualityIsPresentForPosition(4, CimCode.QuantityQualityCodeCalculated)
            .QualityIsPresentForPosition(5, CimCode.QuantityQualityCodeNotAvailable)
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Quality_element_is_excluded_if_edi_quantity_quality_is_missing_or_not_available(
        string documentFormat)
    {
        _energyResultMessageTimeSeries
            .WithPoint(new EnergyResultMessagePoint(1, 1, CalculatedQuantityQuality.Missing, "2022-12-12T23:00:00Z"));

        _energyResultMessageTimeSeries
            .WithPoint(new EnergyResultMessagePoint(2, 1, CalculatedQuantityQuality.NotAvailable, "2022-12-12T23:01:00Z"));

        var document = await CreateDocument(_energyResultMessageTimeSeries, DocumentFormat.FromName(documentFormat));

        // Assert
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .QualityIsNotPresentForPosition(1)
            .QualityIsNotPresentForPosition(2)
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Quality_element_has_correct_code_for_ebix_formats(string documentFormat)
    {
        var points = Enum.GetValues(typeof(CalculatedQuantityQuality))
            .Cast<CalculatedQuantityQuality>()
            .Order()
            .Where(x => x != CalculatedQuantityQuality.Missing)
            .Where(x => x != CalculatedQuantityQuality.NotAvailable)
            .Select((quality, index) => new EnergyResultMessagePoint(index, 1, quality, "2022-12-12T23:00:00Z"))
            .ToList();

        points.ForEach(point => _energyResultMessageTimeSeries.WithPoint(point));

        var document = await CreateDocument(_energyResultMessageTimeSeries, DocumentFormat.FromName(documentFormat));

        // Assert
        points.Should().HaveCount(4);
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .QualityIsPresentForPosition(1, EbixCode.QuantityQualityCodeEstimated)
            .QualityIsPresentForPosition(2, EbixCode.QuantityQualityCodeEstimated)
            .QualityIsPresentForPosition(3, EbixCode.QuantityQualityCodeMeasured)
            .QualityIsPresentForPosition(4, EbixCode.QuantityQualityCodeMeasured)
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix))]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Settlement_method_is_excluded(string documentFormat)
    {
        _energyResultMessageTimeSeries
            .WithMeteringPointType(MeteringPointType.Production)
            .WithSettlementMethod(null);

        var document = await CreateDocument(_energyResultMessageTimeSeries, DocumentFormat.FromName(documentFormat));

        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .SettlementMethodIsNotPresent()
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix))]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Energy_supplier_number_is_excluded(string documentFormat)
    {
        _energyResultMessageTimeSeries
            .WithEnergySupplierNumber(null);

        var document = await CreateDocument(_energyResultMessageTimeSeries, DocumentFormat.FromName(documentFormat));

        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .EnergySupplierNumberIsNotPresent()
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix))]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Balance_responsible_number_is_excluded(string documentFormat)
    {
        _energyResultMessageTimeSeries
            .WithBalanceResponsibleNumber(null);

        var document = await CreateDocument(_energyResultMessageTimeSeries, DocumentFormat.FromName(documentFormat));

        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .BalanceResponsibleNumberIsNotPresent()
            .DocumentIsValidAsync();
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
        _energyResultMessageTimeSeries.WithBusinessReason(BusinessReason.FromName(processType));

        var document = await CreateDocument(_energyResultMessageTimeSeries, DocumentFormat.FromName(documentFormat));

        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasBusinessReason(BusinessReason.FromName(processType))
            .SettlementVersionIsNotPresent()
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Ebix), nameof(BusinessReason.Correction), nameof(SettlementVersion.FirstCorrection))]
    [InlineData(nameof(DocumentFormat.Xml), nameof(BusinessReason.Correction), nameof(SettlementVersion.FirstCorrection))]
    [InlineData(nameof(DocumentFormat.Json), nameof(BusinessReason.Correction), nameof(SettlementVersion.FirstCorrection))]
    public async Task Business_reason_and_settlement_version_is_translated(string documentFormat, string processType, string settlementVersion)
    {
        _energyResultMessageTimeSeries
            .WithMessageId(SampleData.MessageId)
            .WithTransactionId(SampleData.TransactionId)
            .WithBusinessReason(BusinessReason.FromName(processType))
            .WithSettlementVersion(SettlementVersion.FromName(settlementVersion))
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .WithPoint(new EnergyResultMessagePoint(1, 1m, CalculatedQuantityQuality.Calculated, "2022-12-12T23:00:00Z"));

        var document = await CreateDocument(_energyResultMessageTimeSeries, DocumentFormat.FromName(documentFormat));

        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasBusinessReason(BusinessReason.FromName(processType))
            .HasSettlementVersion(SettlementVersion.FromName(settlementVersion))
            .DocumentIsValidAsync();
    }

    [Fact]
    public async Task Can_create_ebix_document_with_36_chars_transaction_id()
    {
        // Arrange
        var transactionId = TransactionId.From("93dbd8bb-4fbb-4b9d-b57f-f6a5c16f7bdf");
        var originalTransactionId = TransactionId.From("b340db36-ef97-4515-839c-1d8b544e9174");

        _energyResultMessageTimeSeries
            .WithTransactionId(transactionId)
            .WithOriginalTransactionIdReference(originalTransactionId);

        // Act
        var document = await CreateDocument(_energyResultMessageTimeSeries, DocumentFormat.Ebix);

        // Assert
        var assertions = await new AssertNotifyAggregatedMeasureDataEbixDocument(
                AssertEbixDocument.Document(document.Stream, "ns0", _documentValidation.Validator))
            .HasStructureValidationErrorsAsync(
            [
                $"The value '{transactionId.Value}' is invalid according to its datatype",
                $"The value '{originalTransactionId.Value}' is invalid according to its datatype",
            ]);

        assertions.HasTransactionId(transactionId).HasOriginalTransactionIdReference(originalTransactionId);
    }

    private Task<MarketDocumentStream> CreateDocument(EnergyResultMessageTimeSeriesBuilder resultBuilder, DocumentFormat documentFormat)
    {
        var documentHeader = resultBuilder.BuildHeader();
        var records = _parser.From(resultBuilder.BuildTimeSeries());
        if (documentFormat == DocumentFormat.Ebix)
        {
            return new NotifyAggregatedMeasureDataEbixDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records, });
        }
        else if (documentFormat == DocumentFormat.Xml)
        {
            return new NotifyAggregatedMeasureDataCimXmlDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records, });
        }
        else
        {
            return new NotifyAggregatedMeasureDataCimJsonDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records, });
        }
    }

    private IAssertNotifyAggregatedMeasureDataDocument AssertDocument(MarketDocumentStream document, DocumentFormat documentFormat) => AssertDocument(document.Stream, documentFormat);

    private IAssertNotifyAggregatedMeasureDataDocument AssertDocument(Stream document, DocumentFormat documentFormat)
    {
        if (documentFormat == DocumentFormat.Ebix)
        {
            var assertEbixDocument = AssertEbixDocument.Document(document, "ns0", _documentValidation.Validator);
            return new AssertNotifyAggregatedMeasureDataEbixDocument(assertEbixDocument);
        }
        else if (documentFormat == DocumentFormat.Xml)
        {
            var assertXmlDocument = AssertXmlDocument.Document(document, "cim", _documentValidation.Validator);
            return new AssertNotifyAggregatedMeasureDataXmlDocument(assertXmlDocument);
        }
        else
        {
            return new AssertNotifyAggregatedMeasureDataJsonDocument(document);
        }
    }
}
