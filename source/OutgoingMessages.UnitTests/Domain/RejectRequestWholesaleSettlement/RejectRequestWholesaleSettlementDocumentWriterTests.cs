﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Text.Encodings.Web;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RejectRequestWholesaleSettlement;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Asserts;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RejectRequestWholesaleSettlement;

public sealed class RejectRequestWholesaleSettlementDocumentWriterTests : IClassFixture<DocumentValidationFixture>
{
    private readonly DocumentValidationFixture _documentValidation;
    private readonly MessageRecordParser _parser;
    private readonly RejectedWholesaleServicesMessageBuilder _rejectedEnergyResultMessageBuilder;

    public RejectRequestWholesaleSettlementDocumentWriterTests(DocumentValidationFixture documentValidation)
    {
        _documentValidation = documentValidation;
        _parser = new MessageRecordParser(new Serializer());
        _rejectedEnergyResultMessageBuilder = RejectedWholesaleServicesMessageBuilder.RejectWholesaleService();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Given_RejectRequestWholesaleSettlementDocument_When_CreateDocument_Then_DocumentCreated(
        string documentFormat)
    {
        var marketDocumentStream = await CreateDocument(
            _rejectedEnergyResultMessageBuilder,
            DocumentFormat.FromName(documentFormat));

        AssertDocument(marketDocumentStream.Stream, DocumentFormat.FromName(documentFormat))
            .HasMessageId(SampleData.MessageId)
            .HasSenderId(SampleData.SenderId)
            .HasReceiverId(SampleData.ReceiverId)
            .HasTimestamp(SampleData.CreationDate)
            .HasReasonCode(SampleData.ReasonCode)
            .HasBusinessReason(SampleData.BusinessReason)
            .HasTransactionId(SampleData.TransactionId)
            .HasSerieReasonCode(SampleData.SerieReasonCode)
            .HasSerieReasonMessage(SampleData.SerieReasonMessage)
            .HasOriginalTransactionId(SampleData.OriginalTransactionId);

        marketDocumentStream.Stream.Position = 0;
        var validateDocument = () =>
            AssertDocument(marketDocumentStream.Stream, DocumentFormat.FromName(documentFormat)).DocumentIsValidAsync();

        if (documentFormat == nameof(DocumentFormat.Ebix))
        {
            (await validateDocument.Should()
                .ThrowAsync<Exception>())
                .Which
                .Message
                .Should()
                .Contain("OriginalBusinessDocument")
                .And
                .Contain(SampleData.OriginalTransactionId.Value)
                .And
                .Contain("The actual length is greater than the MaxLength value");
        }
        else
        {
            await validateDocument.Should().NotThrowAsync();
        }
    }

    private Task<MarketDocumentStream> CreateDocument(
        RejectedWholesaleServicesMessageBuilder resultBuilder,
        DocumentFormat documentFormat)
    {
        var documentHeader = resultBuilder.BuildHeader();
        var records = _parser.From(resultBuilder.BuildRejectedTimeSerie());
        if (documentFormat == DocumentFormat.Ebix)
        {
            return new RejectRequestWholesaleSettlementEbixDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records });
        }

        if (documentFormat == DocumentFormat.Xml)
        {
            return new RejectRequestWholesaleSettlementCimXmlDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records });
        }

        var serviceProvider = new ServiceCollection().AddJavaScriptEncoder().BuildServiceProvider();
        return new RejectRequestWholesaleSettlementCimJsonDocumentWriter(
                _parser,
                serviceProvider.GetRequiredService<JavaScriptEncoder>())
            .WriteAsync(documentHeader, new[] { records });
    }

    private IAssertRejectRequestWholesaleSettlementDocument AssertDocument(
        Stream document,
        DocumentFormat documentFormat)
    {
        if (documentFormat == DocumentFormat.Ebix)
        {
            var assertEbixDocument = AssertEbixDocument.Document(document, "ns0", _documentValidation.Validator);
            return new AssertRejectRequestWholesaleSettlementEbixDocument(assertEbixDocument);
        }

        if (documentFormat == DocumentFormat.Xml)
        {
            var assertXmlDocument = AssertXmlDocument.Document(document, "cim", _documentValidation.Validator);
            return new AssertRejectRequestWholesaleSettlementXmlDocument(assertXmlDocument);
        }

        return new AssertRejectRequestWholesaleSettlementJsonDocument(document);
    }
}
