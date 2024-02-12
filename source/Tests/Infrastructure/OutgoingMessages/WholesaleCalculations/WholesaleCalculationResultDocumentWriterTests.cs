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
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.WholesaleCalculations;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
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
    public async Task Can_create_notifyWholesaleServices_document(string documentFormat)
    {
        var document = await CreateDocument(
            _wholesaleCalculationsResultMessageBuilder
                .WithMessageId(SampleData.MessageId.ToString())
                .WithTimestamp(SampleData.Timestamp)
                .WithSender(SampleData.SenderId, ActorRole.EnergySupplier)
                .WithReceiver(SampleData.ReceiverId, ActorRole.MeteredDataAdministrator) // TODO: Unsure about this

                .WithTransactionId(SampleData.TransactionId)
                .WithGridArea(SampleData.GridAreaCode)
                .WithEnergySupplierNumber(SampleData.EnergySupplier)
                .WithPeriod(SampleData.PeriodStartUtc, SampleData.PeriodEndUtc)
                .WithCurrency(SampleData.Currency)
                .WithMeasurementUnit(SampleData.MeasurementUnit)
                .WithBusinessReason(SampleData.BusinessReason),
            DocumentFormat.From(documentFormat));

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
                .HasGridAreaCode(SampleData.GridAreaCode)
                .HasEnergySupplierNumber(SampleData.EnergySupplier)
                .HasPeriod(new Period(SampleData.PeriodStartUtc, SampleData.PeriodEndUtc))
                .HasCurrency(SampleData.Currency)
                .HasMeasurementUnit(SampleData.MeasurementUnit)
            .DocumentIsValidAsync()
            ;
    }

    private Task<MarketDocumentStream> CreateDocument(WholesaleCalculationsResultMessageBuilder resultBuilder, DocumentFormat documentFormat)
    {
        var documentHeader = resultBuilder.BuildHeader();
        var records = _parser.From(resultBuilder.BuildWholesaleCalculation());

        if (documentFormat == DocumentFormat.Xml)
        {
            return new WholesaleCalculationXmlDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records, });
        }

        throw new NotImplementedException();
    }

    // IAssertWholesaleCalculationResultDocument
    private AssertWholesaleCalculationResultXmlDocument AssertDocument(MarketDocumentStream document, DocumentFormat documentFormat)
    {
         if (documentFormat == DocumentFormat.Xml)
         {
             var assertXmlDocument = AssertXmlDocument.Document(document.Stream, "cim", _documentValidation.Validator);
             return new AssertWholesaleCalculationResultXmlDocument(assertXmlDocument);
         }

         throw new NotSupportedException($"Document format '{documentFormat}' is not supported");
    }
}
