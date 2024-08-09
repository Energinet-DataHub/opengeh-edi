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

using System.Collections.ObjectModel;
using System.Globalization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyWholesaleServices;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.EDI.Tests.Factories;
using Energinet.DataHub.EDI.Tests.Fixtures;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using FluentAssertions.Execution;
using NodaTime.Text;
using Xunit;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;
using SettlementVersion = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.SettlementVersion;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;

public class NotifyWholesaleServicesDocumentWriterTests : IClassFixture<DocumentValidationFixture>
{
    private readonly DocumentValidationFixture _documentValidation;
    private readonly MessageRecordParser _parser;
    private readonly WholesaleServicesSeriesBuilder _wholesaleServicesSeriesBuilder;

    public NotifyWholesaleServicesDocumentWriterTests(
        DocumentValidationFixture documentValidation)
    {
        _documentValidation = documentValidation;
        _parser = new MessageRecordParser(new Serializer());
        _wholesaleServicesSeriesBuilder = new WholesaleServicesSeriesBuilder();
    }

    public static IEnumerable<object[]> AllDocumentFormatsWithMeteringPointTypes()
    {
        var documentFormats = EnumerationType.GetAll<DocumentFormat>();
        var meteringPointTypes = EnumerationType.GetAll<MeteringPointType>();

        return documentFormats
            .SelectMany(df => meteringPointTypes
                    .Select(mpt => new object[] { df, mpt }));
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document(string documentFormat)
    {
        // Arrange
        var messageBuilder = _wholesaleServicesSeriesBuilder
            .WithMessageId(SampleData.MessageId)
            .WithBusinessReason(SampleData.BusinessReason)
            .WithTimestamp(SampleData.Timestamp)
            .WithSender(SampleData.SenderId, ActorRole.EnergySupplier)
            .WithReceiver(SampleData.ReceiverId, ActorRole.MeteredDataResponsible)
            .WithTransactionId(SampleData.TransactionId)
            .WithCalculationVersion(SampleData.Version)
            .WithChargeCode(SampleData.ChargeCode)
            .WithChargeType(SampleData.ChargeType)
            .WithChargeOwner(SampleData.ChargeOwner)
            .WithGridArea(SampleData.GridAreaCode)
            .WithEnergySupplier(SampleData.EnergySupplier)
            .WithPeriod(SampleData.PeriodStartUtc, SampleData.PeriodEndUtc)
            .WithCurrency(SampleData.Currency)
            .WithMeasurementUnit(SampleData.MeasurementUnit)
            .WithPriceMeasurementUnit(SampleData.PriceMeasureUnit)
            .WithResolution(SampleData.Resolution)
            .WithOriginalTransactionIdReference(SampleData.TransactionId)
            .WithPoints(new Collection<WholesaleServicesPoint>() { new(1, 1, 1, SampleData.Quantity, CalculatedQuantityQuality.Calculated) });

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildWholesaleCalculation(),
            DocumentFormat.FromName(documentFormat));

        // Assert
        using var assertionScope = new AssertionScope();
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasMessageId(SampleData.MessageId)
            .HasBusinessReason(SampleData.BusinessReason, CodeListType.EbixDenmark) // "D05" (WholesaleFixing) is from CodeListType.EbixDenmark
            .HasSenderId(SampleData.SenderId, "A10")
            .HasSenderRole(ActorRole.EnergySupplier)
            .HasReceiverId(SampleData.ReceiverId)
            .HasReceiverRole(ActorRole.MeteredDataResponsible, CodeListType.Ebix) // MDR is from CodeListType.Ebix
            .HasTimestamp(SampleData.Timestamp)
            .HasTransactionId(SampleData.TransactionId)
            .HasCalculationVersion(SampleData.Version)
            .HasChargeCode(SampleData.ChargeCode)
            .HasChargeType(SampleData.ChargeType)
            .HasChargeTypeOwner(SampleData.ChargeOwner, "A01")
            .HasGridAreaCode(SampleData.GridAreaCode, "NDK")
            .HasEnergySupplierNumber(SampleData.EnergySupplier, "A10")
            .HasPeriod(new Period(SampleData.PeriodStartUtc, SampleData.PeriodEndUtc))
            .HasCurrency(SampleData.Currency)
            .HasQuantityMeasurementUnit(SampleData.MeasurementUnit)
            .HasPriceMeasurementUnit(SampleData.PriceMeasureUnit)
            .HasResolution(SampleData.Resolution)
            .HasSumQuantityForPosition(1, SampleData.Quantity)
            .HasProductCode(ProductType.Tariff.Code)
            .HasOriginalTransactionIdReference(SampleData.TransactionId)
            .SettlementVersionDoesNotExist()
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_with_optional_values_as_null(string documentFormat)
    {
        // Arrange
        // This is the wholesale series with most nullable fields.
        var series = new WholesaleServicesSeries(
            TransactionId: SampleData.TransactionId,
            CalculationVersion: 1,
            GridAreaCode: SampleData.GridAreaCode,
            EnergySupplier: SampleData.EnergySupplier,
            Period: new Period(SampleData.PeriodStartUtc, SampleData.PeriodEndUtc),
            SettlementVersion: null,
            QuantityMeasureUnit: MeasurementUnit.Kwh,
            Currency: Currency.DanishCrowns,
            Resolution: Resolution.Monthly,
            Points: new Collection<WholesaleServicesPoint> { new(1, null, null, 100, null) },
            ChargeCode: null,
            IsTax: false,
            ChargeOwner: null,
            PriceMeasureUnit: null,
            ChargeType: null,
            MeteringPointType: MeteringPointType.Consumption,
            SettlementMethod: null,
            OriginalTransactionIdReference: null);
        var header = new OutgoingMessageHeader(
            DataHubNames.BusinessReason.WholesaleFixing,
            SampleData.SenderId.Value,
            ActorRole.EnergySupplier.Code,
            SampleData.ReceiverId.Value,
            ActorRole.EnergySupplier.Code,
            SampleData.MessageId,
            InstantPattern.General.Parse(SampleData.Timestamp).Value);

        // Act
        var document = await WriteDocument(
            header,
            series,
            DocumentFormat.FromName(documentFormat));

        // Assert
        using var assertionScope = new AssertionScope();
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasMessageId(SampleData.MessageId)
            .HasBusinessReason(SampleData.BusinessReason, CodeListType.EbixDenmark) // "D05" (WholesaleFixing) is from CodeListType.EbixDenmark
            .HasSenderId(SampleData.SenderId, "A10")
            .HasSenderRole(ActorRole.EnergySupplier)
            .HasReceiverId(SampleData.ReceiverId)
            .HasReceiverRole(ActorRole.EnergySupplier, CodeListType.Ebix) // MDR is from CodeListType.Ebix
            .HasTimestamp(SampleData.Timestamp)
            .HasTransactionId(SampleData.TransactionId)
            .HasCalculationVersion(SampleData.Version)
            .ChargeCodeDoesNotExist()
            .ChargeTypeDoesNotExist()
            .ChargeTypeOwnerDoesNotExist()
            .HasGridAreaCode(SampleData.GridAreaCode, "NDK")
            .HasEnergySupplierNumber(SampleData.EnergySupplier, "A10")
            .HasPeriod(new Period(SampleData.PeriodStartUtc, SampleData.PeriodEndUtc))
            .HasCurrency(SampleData.Currency)
            .HasQuantityMeasurementUnit(SampleData.MeasurementUnit)
            .PriceMeasurementUnitDoesNotExist()
            .HasResolution(Resolution.Monthly)
            .HasSinglePointWithAmountAndQuality(
                new DecimalValue()
                    {
                        Nanos = 0,
                        Units = 100,
                    },
                null)
            .HasProductCode(ProductType.Tariff.Code)
            .OriginalTransactionIdReferenceDoesNotExist()
            .SettlementVersionDoesNotExist()
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_without_energySum_quantity(string documentFormat)
    {
        // Arrange
        var messageBuilder = _wholesaleServicesSeriesBuilder
            .WithPoints(new Collection<WholesaleServicesPoint>() { new(1, 1, 1, null, CalculatedQuantityQuality.Missing) });

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildWholesaleCalculation(),
            DocumentFormat.FromName(documentFormat));

        // Assert
        using var assertionScope = new AssertionScope();
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasSumQuantityForPosition(1, 0)
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_with_settlement_version(string documentFormat)
    {
        // Arrange
        var messageBuilder = _wholesaleServicesSeriesBuilder
            .WithSettlementVersion(SettlementVersion.FirstCorrection);

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildWholesaleCalculation(),
            DocumentFormat.FromName(documentFormat));

        // Assert
        using var assertionScope = new AssertionScope();
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasSettlementVersion(SettlementVersion.FirstCorrection)
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_with_measurement_unit_pieces(string documentFormat)
    {
        // Arrange
        var messageBuilder = _wholesaleServicesSeriesBuilder
            .WithMeasurementUnit(MeasurementUnit.Pieces);

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildWholesaleCalculation(),
            DocumentFormat.FromName(documentFormat));

        // Assert
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasQuantityMeasurementUnit(MeasurementUnit.Pieces)
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_with_calculated_hourly_tariff_amounts_for_flex_consumption(string documentFormat)
    {
        // Arrange
        var firstPoint = new WholesaleServicesPoint(1, 1, 100, 100, CalculatedQuantityQuality.Missing);
        var secondPoint = new WholesaleServicesPoint(2, 1, 200, 200, CalculatedQuantityQuality.Missing);

        var messageBuilder = _wholesaleServicesSeriesBuilder
            .WithSettlementMethod(SettlementMethod.Flex)
            .WithMeteringPointType(MeteringPointType.Consumption)
            .WithResolution(Resolution.Hourly)
            .WithPoints(new()
            {
                firstPoint,
                secondPoint,
            });

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildWholesaleCalculation(),
            DocumentFormat.FromName(documentFormat));

        // Assert
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasSettlementMethod(SettlementMethod.Flex)
            .HasMeteringPointType(MeteringPointType.Consumption)
            .HasResolution(Resolution.Hourly)
            .HasPriceForPosition(1, firstPoint.Price?.ToString(NumberFormatInfo.InvariantInfo))
            .HasPriceForPosition(2, secondPoint.Price?.ToString(NumberFormatInfo.InvariantInfo))
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_with_calculated_hourly_tariff_amounts_for_production(string documentFormat)
    {
        // Arrange
        var firstPoint = new WholesaleServicesPoint(1, 1, 100, 100, CalculatedQuantityQuality.Missing);
        var secondPoint = new WholesaleServicesPoint(2, 1, 200, 100, CalculatedQuantityQuality.Missing);

        var messageBuilder = _wholesaleServicesSeriesBuilder
                .WithSettlementMethod(null)
                .WithMeteringPointType(MeteringPointType.Production)
                .WithResolution(Resolution.Hourly)
                .WithPoints(new()
                {
                    firstPoint,
                    secondPoint,
                });

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildWholesaleCalculation(),
            DocumentFormat.FromName(documentFormat));

        // Assert
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .SettlementMethodDoesNotExist()
            .HasMeteringPointType(MeteringPointType.Production)
            .HasResolution(Resolution.Hourly)
            .HasPriceForPosition(1, firstPoint.Price?.ToString(NumberFormatInfo.InvariantInfo))
            .HasPriceForPosition(2, secondPoint.Price?.ToString(NumberFormatInfo.InvariantInfo))
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_with_calculated_hourly_tariff_amounts_for_consumption(string documentFormat)
    {
        // Arrange
        var firstPoint = new WholesaleServicesPoint(1, 1, 100, 100, CalculatedQuantityQuality.Missing);
        var secondPoint = new WholesaleServicesPoint(2, 1, 200, 200, CalculatedQuantityQuality.Missing);

        var messageBuilder = _wholesaleServicesSeriesBuilder
            .WithSettlementMethod(SettlementMethod.NonProfiled)
            .WithMeteringPointType(MeteringPointType.Consumption)
            .WithResolution(Resolution.Hourly)
            .WithPoints(new()
            {
                firstPoint,
                secondPoint,
            });

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildWholesaleCalculation(),
            DocumentFormat.FromName(documentFormat));

        // Assert
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasSettlementMethod(SettlementMethod.NonProfiled)
            .HasMeteringPointType(MeteringPointType.Consumption)
            .HasResolution(Resolution.Hourly)
            .HasPriceForPosition(1, firstPoint.Price?.ToString(NumberFormatInfo.InvariantInfo))
            .HasPriceForPosition(2, secondPoint.Price?.ToString(NumberFormatInfo.InvariantInfo))
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_with_hourly_resolution(string documentFormat)
    {
        // Arrange
        var messageBuilder = _wholesaleServicesSeriesBuilder
            .WithResolution(Resolution.Hourly);

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildWholesaleCalculation(),
            DocumentFormat.FromName(documentFormat));

        // Assert
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasResolution(Resolution.Hourly)
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_with_daily_resolution(string documentFormat)
    {
        // Arrange
        var messageBuilder = _wholesaleServicesSeriesBuilder
            .WithResolution(Resolution.Daily);

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildWholesaleCalculation(),
            DocumentFormat.FromName(documentFormat));

        // Assert
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasResolution(Resolution.Daily)
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Given_CalculatedQuantityQuality_When_writingDocument_Then_HasExpectedQuantityQuality(string documentFormat)
    {
        var missingQuantityQualityPoint = new WholesaleServicesPoint(1, 1, 100, 100, CalculatedQuantityQuality.Missing);
        var incompleteQuantityQualityPoint = new WholesaleServicesPoint(2, 1, 100, 100, CalculatedQuantityQuality.Incomplete);
        var calculatedQuantityQualityPoint = new WholesaleServicesPoint(3, 1, 100, 100, CalculatedQuantityQuality.Calculated);
        var notAvaliableQuantityQualityPoint = new WholesaleServicesPoint(4, 1, 100, 100, CalculatedQuantityQuality.NotAvailable);

        var point = new List<WholesaleServicesPoint>()
        {
            missingQuantityQualityPoint,
            incompleteQuantityQualityPoint,
            calculatedQuantityQualityPoint,
            notAvaliableQuantityQualityPoint,
        };

        // Arrange
        var messageBuilder = _wholesaleServicesSeriesBuilder
            .WithPoints(new()
            {
                missingQuantityQualityPoint,
                incompleteQuantityQualityPoint,
                calculatedQuantityQualityPoint,
                notAvaliableQuantityQualityPoint,
            });

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildWholesaleCalculation(),
            DocumentFormat.FromName(documentFormat));

        // Assert
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasPoints(point)
            .DocumentIsValidAsync();
    }

    [Theory]
    [MemberData(nameof(AllDocumentFormatsWithMeteringPointTypes))]
    public async Task Can_create_notifyWholesaleServices_document_with_all_metering_point_types(DocumentFormat documentFormat, MeteringPointType meteringPointType)
    {
        // Arrange
        var messageBuilder = _wholesaleServicesSeriesBuilder
            .WithMeteringPointType(meteringPointType);

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildWholesaleCalculation(),
            documentFormat);

        // Assert
        await AssertDocument(document, documentFormat)
            .HasMeteringPointType(meteringPointType)
            .DocumentIsValidAsync();
    }

    [Fact]
    public async Task Can_support_existing_ebix_documents_with_36_char_ids()
    {
        // Arrange
        var messageId = MessageId.Create("26be9856-db4c-451b-a275-18d5fa364285");
        var transactionId = TransactionId.From("93dbd8bb-4fbb-4b9d-b57f-f6a5c16f7bdf");
        var originalTransactionId = TransactionId.From("b340db36-ef97-4515-839c-1d8b544e9174");

        var messageBuilder = _wholesaleServicesSeriesBuilder
            .WithMessageId(messageId.Value)
            .WithTransactionId(transactionId)
            .WithOriginalTransactionIdReference(originalTransactionId);

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildWholesaleCalculation(),
            DocumentFormat.Ebix);

        // Assert
        var assertions = await new AssertNotifyWholesaleServicesEbixDocument(
                AssertEbixDocument.Document(document.Stream, "ns0", _documentValidation.Validator))
            .HasStructureValidationErrorsAsync(
            [
                $"The value '{messageId.Value}' is invalid according to its datatype",
                $"The value '{transactionId.Value}' is invalid according to its datatype",
                $"The value '{originalTransactionId.Value}' is invalid according to its datatype",
            ]);

        assertions
            .HasMessageId(messageId.Value)
            .HasTransactionId(transactionId)
            .HasOriginalTransactionIdReference(originalTransactionId);
    }

    private Task<MarketDocumentStream> WriteDocument(OutgoingMessageHeader header, WholesaleServicesSeries wholesaleServicesSeries, DocumentFormat documentFormat)
    {
        var records = _parser.From(wholesaleServicesSeries);

        if (documentFormat == DocumentFormat.Xml)
        {
            return new NotifyWholesaleServicesCimXmlDocumentWriter(_parser).WriteAsync(header, new[] { records });
        }

        if (documentFormat == DocumentFormat.Ebix)
        {
            return new NotifyWholesaleServicesEbixDocumentWriter(_parser).WriteAsync(header, new[] { records });
        }

        if (documentFormat == DocumentFormat.Json)
        {
            return new NotifyWholesaleServicesCimJsonDocumentWriter(_parser).WriteAsync(
                header,
                new[] { records });
        }

        throw new NotImplementedException();
    }

    private IAssertNotifyWholesaleServicesDocument AssertDocument(MarketDocumentStream document, DocumentFormat documentFormat)
    {
         if (documentFormat == DocumentFormat.Xml)
         {
             var assertXmlDocument = AssertXmlDocument.Document(document.Stream, "cim", _documentValidation.Validator);
             return new AssertNotifyWholesaleServicesXmlDocument(assertXmlDocument);
         }

         if (documentFormat == DocumentFormat.Json)
         {
             return new AssertNotifyWholesaleServicesJsonDocument(document.Stream);
         }

         if (documentFormat == DocumentFormat.Ebix)
         {
             var assertEbixDocument = AssertEbixDocument.Document(document.Stream, "ns0", _documentValidation.Validator);
             return new AssertNotifyWholesaleServicesEbixDocument(assertEbixDocument);
         }

         throw new NotSupportedException($"Document format '{documentFormat}' is not supported");
    }
}
