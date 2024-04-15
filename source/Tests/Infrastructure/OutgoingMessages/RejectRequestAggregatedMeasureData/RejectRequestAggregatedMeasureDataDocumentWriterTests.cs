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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Application;
using Energinet.DataHub.EDI.OutgoingMessages.Application.DocumentWriters.RejectRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.Tests.Factories;
using Energinet.DataHub.EDI.Tests.Fixtures;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RejectRequestAggregatedMeasureData;

public class RejectRequestAggregatedMeasureDataDocumentWriterTests : IClassFixture<DocumentValidationFixture>
{
    private readonly DocumentValidationFixture _documentValidation;
    private readonly MessageRecordParser _parser;
    private readonly RejectedEnergyResultMessageSerieBuilder _rejectedEnergyResultMessageSerie;

    public RejectRequestAggregatedMeasureDataDocumentWriterTests(DocumentValidationFixture documentValidation)
    {
        _documentValidation = documentValidation;
        _parser = new MessageRecordParser(new Serializer());
        _rejectedEnergyResultMessageSerie = RejectedEnergyResultMessageSerieBuilder
            .RejectAggregatedMeasureDataResult();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_document(string documentFormat)
    {
        var marketDocumentStream = await CreateDocument(
                _rejectedEnergyResultMessageSerie,
                DocumentFormat.FromName(documentFormat));

        await AssertDocument(marketDocumentStream.Stream, DocumentFormat.FromName(documentFormat))
            .HasMessageId(SampleData.MessageId)
            .HasSenderId(SampleData.SenderId)
            .HasReceiverId(SampleData.ReceiverId)
            .HasTimestamp(SampleData.CreationDate)
            .HasReasonCode(SampleData.ReasonCode)
            .HasBusinessReason(SampleData.BusinessReason)
            .HasTransactionId(SampleData.TransactionId)
            .HasSerieReasonCode(SampleData.SerieReasonCode)
            .HasSerieReasonMessage(SampleData.SerieReasonMessage)
            .HasOriginalTransactionId(SampleData.OriginalTransactionId)
            .DocumentIsValidAsync();
    }

    private Task<MarketDocumentStream> CreateDocument(RejectedEnergyResultMessageSerieBuilder resultBuilder, DocumentFormat documentFormat)
    {
        var documentHeader = resultBuilder.BuildHeader();
        var records = _parser.From(resultBuilder.BuildRejectedTimeSerie());
        if (documentFormat == DocumentFormat.Ebix)
        {
            return new RejectRequestAggregatedMeasureDataEbixDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records, });
        }
        else if (documentFormat == DocumentFormat.Xml)
        {
            return new RejectRequestAggregatedMeasureDataXmlXmlDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records, });
        }
        else
        {
            return new RejectRequestAggregatedMeasureDataJsonDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records, });
        }
    }

    private IAssertRejectRequestAggregatedMeasureDataDocument AssertDocument(Stream document, DocumentFormat documentFormat)
    {
        if (documentFormat == DocumentFormat.Ebix)
        {
            var assertEbixDocument = AssertEbixDocument.Document(document, "ns0", _documentValidation.Validator);
            return new AssertRejectRequestAggregatedMeasureDataEbixDocument(assertEbixDocument);
        }
        else if (documentFormat == DocumentFormat.Xml)
        {
            var assertXmlDocument = AssertXmlDocument.Document(document, "cim", _documentValidation.Validator);
            return new AssertRejectRequestAggregatedMeasureDataXmlDocument(assertXmlDocument);
        }
        else
        {
            return new AssertRejectRequestAggregatedMeasureDataJsonDocument(document);
        }
    }
}
