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
using Energinet.DataHub.EDI.Infrastructure.Configuration.Serialization;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.AggregationResult;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.RejectRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Tests.Factories;
using Energinet.DataHub.EDI.Tests.Fixtures;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.AggregationResult;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;
using DocumentFormat = Energinet.DataHub.EDI.Domain.Documents.DocumentFormat;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RejectRequestAggregatedMeasureData;

public class RejectRequestAggregatedMeasureDataResultDocumentWriterTests : IClassFixture<DocumentValidationFixture>
{
    private readonly DocumentValidationFixture _documentValidation;
    private readonly IMessageRecordParser _parser;
    private readonly RejectedTimeSeriesBuilder _rejectedTimeSeries;

    public RejectRequestAggregatedMeasureDataResultDocumentWriterTests(DocumentValidationFixture documentValidation)
    {
        _documentValidation = documentValidation;
        _parser = new MessageRecordParser(new Serializer());
        _rejectedTimeSeries = RejectedTimeSeriesBuilder
            .RejectAggregatedMeasureDataResult();
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_document(string documentFormat)
    {
        var document = await CreateDocument(
                _rejectedTimeSeries,
                DocumentFormat.From(documentFormat));

        await AssertDocument(document, DocumentFormat.From(documentFormat))
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

    private async Task<Stream> CreateDocument(RejectedTimeSeriesBuilder resultBuilder, DocumentFormat documentFormat)
    {
        var documentHeader = resultBuilder.BuildHeader();
        var records = _parser.From(resultBuilder.BuildRejectedTimeSerie());
        if (documentFormat == DocumentFormat.Ebix)
        {
            var writer = new RejectRequestAggregatedMeasureDataEbixDocumentWriter(_parser);
            var payload = await writer.WritePayloadAsync(records).ConfigureAwait(false);

            return await writer.WriteAsync(
                documentHeader,
                new[] { payload, },
                new[] { records, }).ConfigureAwait(false);
        }
        else if (documentFormat == DocumentFormat.Xml)
        {
            var writer = new RejectRequestAggregatedMeasureDataXmlDocumentWriter(_parser);
            var payload = await writer.WritePayloadAsync(records).ConfigureAwait(false);

            return await writer.WriteAsync(
                documentHeader,
                new[] { payload, },
                new[] { records, }).ConfigureAwait(false);
        }
        else
        {
            var writer = new RejectRequestAggregatedMeasureDataJsonDocumentWriter(_parser);
            return await writer.WriteAsync(
                documentHeader,
                new[] { records, },
                new[] { records, }).ConfigureAwait(false);
        }
    }

    private IAssertRejectedAggregatedMeasureDataResultDocument AssertDocument(Stream document, DocumentFormat documentFormat)
    {
        if (documentFormat == DocumentFormat.Ebix)
        {
            var assertEbixDocument = AssertEbixDocument.Document(document, "ns0");
            return new AssertRejectedAggregatedMeasureDataResultEbixDocument(assertEbixDocument);
        }
        else if (documentFormat == DocumentFormat.Xml)
        {
            var assertXmlDocument = AssertXmlDocument.Document(document, "cim", _documentValidation.Validator);
            return new AssertRejectedAggregatedMeasureDataResultXmlDocument(assertXmlDocument);
        }
        else
        {
            return new AssertRejectRequestAggregatedMeasureDataResultJsonDocument(document);
        }
    }
}
