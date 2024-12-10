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

using System.Globalization;
using System.Text.Encodings.Web;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.Tests.Factories;
using Energinet.DataHub.EDI.Tests.Fixtures;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM012;

public class MeteredDateForMeasurementPointDocumentWriterTests(DocumentValidationFixture documentValidation)
    : IClassFixture<DocumentValidationFixture>
{
    private readonly MessageRecordParser _parser = new(new Serializer());
    private readonly MeteredDateForMeasurementPointBuilder _meteredDateForMeasurementPointBuilder = new();
    private readonly DocumentValidationFixture _documentValidation = documentValidation;

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    public async Task Can_create_notifyValidatedMeasureData_document(string documentFormat)
    {
        // Arrange
        var messageBuilder = _meteredDateForMeasurementPointBuilder;

        // Act
        var document = await WriteDocument(
            messageBuilder.BuildHeader(),
            messageBuilder.BuildMeteredDataForMeasurementPoint(),
            DocumentFormat.FromName(documentFormat));

        // Assert
        using var assertionScope = new AssertionScope();
        await AssertDocument(document, DocumentFormat.FromName(documentFormat))
            .MessageIdExists()
            .HasBusinessReason(SampleData.BusinessReason.Code)
            .HasSenderId(SampleData.SenderActorNumber, "A10")
            .HasSenderRole(SampleData.SenderActorRole)
            .HasReceiverId(SampleData.ReceiverActorNumber, "A10")
            .HasReceiverRole(SampleData.ReceiverActorRole)
            .HasTimestamp(SampleData.TimeStamp.ToString())
            .HasTransactionId(1, SampleData.TransactionId)
            .HasMeteringPointNumber(1, SampleData.MeteringPointNumber, "A10")
            .HasMeteringPointType(1, SampleData.MeteringPointType)
            .HasOriginalTransactionIdReferenceId(1, SampleData.OriginalTransactionIdReferenceId?.Value)
            .HasProduct(1, SampleData.Product)
            .HasQuantityMeasureUnit(1, SampleData.QuantityMeasureUnit.Code)
            .HasRegistrationDateTime(1, SampleData.RegistrationDateTime.ToString())
            .HasResolution(1, SampleData.Resolution.Code)
            .HasStartedDateTime(
                1,
                SampleData.StartedDateTime.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture))
            .HasEndedDateTime(
                1,
                SampleData.EndedDateTime.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture))
            .HasPoints(
                1,
                SampleData.Points.Select(
                        p => new AssertPointDocumentFieldsInput(
                            new RequiredPointDocumentFields(p.Position),
                            new OptionalPointDocumentFields(p.Quality, p.Quantity)))
                    .ToList())
            .DocumentIsValidAsync();
    }

    private Task<MarketDocumentStream> WriteDocument(
        OutgoingMessageHeader header,
        MeteredDateForMeasurementPointMarketActivityRecord meteredDateForMeasurementPointMarketActivityRecord,
        DocumentFormat documentFormat)
    {
        var records = _parser.From(meteredDateForMeasurementPointMarketActivityRecord);

        if (documentFormat == DocumentFormat.Xml)
        {
            return new MeteredDateForMeasurementPointCimXmlDocumentWriter(_parser).WriteAsync(header, new[] { records });
        }

        var serviceProvider = new ServiceCollection().AddJavaScriptEncoder().BuildServiceProvider();
        return new MeteredDateForMeasurementPointCimJsonDocumentWriter(
                _parser,
                serviceProvider.GetRequiredService<JavaScriptEncoder>())
            .WriteAsync(header, [records], CancellationToken.None);
    }

    private IAssertMeteredDateForMeasurementPointDocumentDocument AssertDocument(
        MarketDocumentStream document,
        DocumentFormat documentFormat)
    {
        if (documentFormat == DocumentFormat.Xml)
        {
            var assertXmlDocument = AssertXmlDocument.Document(document.Stream, "cim", _documentValidation.Validator);
            return new AssertMeteredDateForMeasurementPointXmlDocument(assertXmlDocument);
        }

        return new AssertMeteredDateForMeasurementPointJsonDocument(document.Stream);
    }
}
