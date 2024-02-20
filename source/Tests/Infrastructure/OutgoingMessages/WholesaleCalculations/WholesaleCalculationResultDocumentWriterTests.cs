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
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Common.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.AggregationResult;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.WholesaleCalculations;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleCalculations;
using Energinet.DataHub.EDI.Tests.Factories;
using Energinet.DataHub.EDI.Tests.Fixtures;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using FluentAssertions.Execution;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.WholesaleCalculations;

public class WholesaleCalculationResultDocumentWriterTests : IClassFixture<DocumentValidationFixture>
{
    private readonly DocumentValidationFixture _documentValidation;
    private readonly MessageRecordParser _parser;
    private readonly WholesaleCalculationsResultMessageBuilder _wholesaleCalculationsResultMessageBuilder;

    public WholesaleCalculationResultDocumentWriterTests(
        DocumentValidationFixture documentValidation)
    {
        _documentValidation = documentValidation;
        _parser = new MessageRecordParser(new Serializer());
        _wholesaleCalculationsResultMessageBuilder = new WholesaleCalculationsResultMessageBuilder();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document(string documentFormat)
    {
        // Arrange
        var messageBuilder = _wholesaleCalculationsResultMessageBuilder
            .WithMessageId(SampleData.MessageId.ToString())
            .WithBusinessReason(SampleData.BusinessReason)
            .WithTimestamp(SampleData.Timestamp)
            .WithSender(SampleData.SenderId, ActorRole.EnergySupplier)
            .WithReceiver(SampleData.ReceiverId, ActorRole.MeteredDataAdministrator)
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
            .WithQuantity(SampleData.Quantity);

        // Act
        var document = await WriteDocument(messageBuilder.BuildHeader(), messageBuilder.BuildWholesaleCalculation(), DocumentFormat.From(documentFormat));

        // Assert
        using var assertionScope = new AssertionScope();
        await AssertDocument(document, DocumentFormat.From(documentFormat))
                .HasMessageId(SampleData.MessageId.ToString())
                .HasBusinessReason(SampleData.BusinessReason)
                .HasSenderId(SampleData.SenderId)
                .HasSenderRole(ActorRole.EnergySupplier)
                .HasReceiverId(SampleData.ReceiverId)
                .HasReceiverRole(ActorRole.MeteredDataAdministrator)
                .HasTimestamp(SampleData.Timestamp)
                .HasTransactionId(SampleData.TransactionId)
                .HasCalculationVersion(SampleData.Version)
                .HasChargeCode(SampleData.ChargeCode)
                .HasChargeType(SampleData.ChargeType)
                .HasChargeTypeOwner(SampleData.ChargeOwner)
                .HasGridAreaCode(SampleData.GridAreaCode)
                .HasEnergySupplierNumber(SampleData.EnergySupplier)
                .HasPeriod(new Period(SampleData.PeriodStartUtc, SampleData.PeriodEndUtc))
                .HasCurrency(SampleData.Currency)
                .HasMeasurementUnit(SampleData.MeasurementUnit)
                .HasPriceMeasurementUnit(SampleData.PriceMeasureUnit)
                .HasResolution(SampleData.Resolution)
                .HasPositionAndQuantity(1, SampleData.Quantity)
                .SettlementVersionIsNotPresent()
            .DocumentIsValidAsync();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_without_quantity(string documentFormat)
    {
        // Arrange
        var messageBuilder = _wholesaleCalculationsResultMessageBuilder
            .WithQuantity(null);

        // Act
        var document = await WriteDocument(messageBuilder.BuildHeader(), messageBuilder.BuildWholesaleCalculation(), DocumentFormat.From(documentFormat));

        // Assert
        using var assertionScope = new AssertionScope();
        AssertDocument(document, DocumentFormat.From(documentFormat))
            .HasPositionAndQuantity(1, 0);
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_with_settlement_version(string documentFormat)
    {
        // Arrange
        var messageBuilder = _wholesaleCalculationsResultMessageBuilder
            .WithSettlementVersion(SettlementVersion.FirstCorrection);

        // Act
        var document = await WriteDocument(messageBuilder.BuildHeader(), messageBuilder.BuildWholesaleCalculation(), DocumentFormat.From(documentFormat));

        // Assert
        using var assertionScope = new AssertionScope();
        AssertDocument(document, DocumentFormat.From(documentFormat))
            .HasSettlementVersion(SettlementVersion.FirstCorrection);
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_notifyWholesaleServices_document_with_measurement_unit_pieces(string documentFormat)
    {
        // Arrange
        var messageBuilder = _wholesaleCalculationsResultMessageBuilder
            .WithMeasurementUnit(MeasurementUnit.Pieces);

        // Act
        var document = await WriteDocument(messageBuilder.BuildHeader(), messageBuilder.BuildWholesaleCalculation(), DocumentFormat.From(documentFormat));

        // Assert
        AssertDocument(document, DocumentFormat.From(documentFormat))
            .HasMeasurementUnit(MeasurementUnit.Pieces);
    }

    private Task<MarketDocumentStream> WriteDocument(OutgoingMessageHeader header, WholesaleCalculationSeries wholesaleCalculationSeries, DocumentFormat documentFormat)
    {
        var records = _parser.From(wholesaleCalculationSeries);

        IDocumentWriter documentWriter;
        if (documentFormat == DocumentFormat.Xml)
            documentWriter = new WholesaleCalculationXmlDocumentWriter(_parser);
        else if (documentFormat == DocumentFormat.Ebix)
            documentWriter = new WholesaleCalculationResultEbixDocumentWriter(_parser);
        else
            throw new NotImplementedException();

        return documentWriter.WriteAsync(
            header,
            new[] { records, });
    }

    private IAssertWholesaleCalculationResultDocument AssertDocument(MarketDocumentStream document, DocumentFormat documentFormat)
    {
         if (documentFormat == DocumentFormat.Xml)
         {
             var assertXmlDocument = AssertXmlDocument.Document(document.Stream, "cim", _documentValidation.Validator);
             return new AssertWholesaleCalculationResultXmlDocument(assertXmlDocument);
         }

         if (documentFormat == DocumentFormat.Ebix)
         {
             var assertEbixDocument = AssertEbixDocument.Document(document.Stream, "ns0", _documentValidation.Validator);
             return new AssertWholesaleCalculationResultEbixDocument(assertEbixDocument);
         }

         throw new NotSupportedException($"Document format '{documentFormat}' is not supported");
    }
}
