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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Application;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyWholesaleServices;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Tests.Factories;
using Energinet.DataHub.EDI.Tests.Fixtures;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using FluentAssertions.Execution;
using Xunit;

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

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document(string documentFormat)
    {
        var transactionId = documentFormat == nameof(DocumentFormat.Ebix)
            ? SampleData.TransactionId.ToString().Substring(0, 35)
            : SampleData.TransactionId.ToString();
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
            .WithOriginalTransactionIdReference(transactionId)
            .WithPoints(new Collection<WholesaleServicesPoint>() { new(1, 1, 1, SampleData.Quantity, null) });

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
            .HasMeasurementUnit(SampleData.MeasurementUnit)
            .HasPriceMeasurementUnit(SampleData.PriceMeasureUnit)
            .HasResolution(SampleData.Resolution)
            .HasPositionAndQuantity(1, SampleData.Quantity)
            .HasProductCode(ProductType.Tariff.Code)
            .HasOriginalTransactionIdReference(transactionId)
            .SettlementVersionIsNotPresent()
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
            .WithPoints(new Collection<WholesaleServicesPoint>() { new(1, 1, 1, null, null) });

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildWholesaleCalculation(),
            DocumentFormat.FromName(documentFormat));

        // Assert
        using var assertionScope = new AssertionScope();
        AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasPositionAndQuantity(1, 0);
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
        AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasSettlementVersion(SettlementVersion.FirstCorrection);
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
        AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasMeasurementUnit(MeasurementUnit.Pieces);
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_with_calculated_hourly_tariff_amounts_for_flex_consumption(string documentFormat)
    {
        // Arrange
        var firstPoint = new WholesaleServicesPoint(1, 1, 100, 100, null);
        var secondPoint = new WholesaleServicesPoint(2, 1, 200, 200, null);

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
        AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasSettlementMethod(SettlementMethod.Flex)
            .HasMeteringPointType(MeteringPointType.Consumption)
            .HasResolution(Resolution.Hourly)
            .PriceAmountIsPresentForPointIndex(0, firstPoint.Price?.ToString(NumberFormatInfo.InvariantInfo))
            .PriceAmountIsPresentForPointIndex(1, secondPoint.Price?.ToString(NumberFormatInfo.InvariantInfo));
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_with_calculated_hourly_tariff_amounts_for_production(string documentFormat)
    {
        // Arrange
        var firstPoint = new WholesaleServicesPoint(1, 1, 100, 100, null);
        var secondPoint = new WholesaleServicesPoint(2, 1, 200, 100, null);

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
        AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .SettlementMethodIsNotPresent()
            .HasMeteringPointType(MeteringPointType.Production)
            .HasResolution(Resolution.Hourly)
            .PriceAmountIsPresentForPointIndex(0, firstPoint.Price?.ToString(NumberFormatInfo.InvariantInfo))
            .PriceAmountIsPresentForPointIndex(1, secondPoint.Price?.ToString(NumberFormatInfo.InvariantInfo));
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_with_calculated_hourly_tariff_amounts_for_consumption(string documentFormat)
    {
        // Arrange
        var firstPoint = new WholesaleServicesPoint(1, 1, 100, 100, null);
        var secondPoint = new WholesaleServicesPoint(2, 1, 200, 200, null);

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
        AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasSettlementMethod(SettlementMethod.NonProfiled)
            .HasMeteringPointType(MeteringPointType.Consumption)
            .HasResolution(Resolution.Hourly)
            .PriceAmountIsPresentForPointIndex(0, firstPoint.Price?.ToString(NumberFormatInfo.InvariantInfo))
            .PriceAmountIsPresentForPointIndex(1, secondPoint.Price?.ToString(NumberFormatInfo.InvariantInfo));
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
        AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasResolution(Resolution.Hourly);
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
        AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .HasResolution(Resolution.Daily);
    }

    private Task<MarketDocumentStream> WriteDocument(OutgoingMessageHeader header, WholesaleServicesSeries wholesaleServicesSeries, DocumentFormat documentFormat)
    {
        var records = _parser.From(wholesaleServicesSeries);

        if (documentFormat == DocumentFormat.Xml)
        {
            return new NotifyWholesaleServicesXmlDocumentWriter(_parser).WriteAsync(header, new[] { records });
        }
        else if (documentFormat == DocumentFormat.Ebix)
        {
            return new NotifyWholesaleServicesEbixDocumentWriter(_parser).WriteAsync(header, new[] { records });
        }
        else if (documentFormat == DocumentFormat.Json)
        {
            return new NotifyWholesaleServicesJsonDocumentWriter(_parser).WriteAsync(
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
