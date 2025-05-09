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

using System.Text.Encodings.Web;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM009;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NodaTime.Text;
using RejectReason = Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint.RejectReason;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM009;

public class AcknowledgementTests : IClassFixture<DocumentValidationFixture>
{
    private readonly MessageRecordParser _parser;

    public AcknowledgementTests(DocumentValidationFixture documentValidation)
    {
        _parser = new MessageRecordParser(new Serializer());
    }

    [Theory]
    [InlineData(nameof(DocumentFormat.Xml))]
    [InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Can_create_document(string documentFormat)
    {
        var rejectMessageBuilder = new RejectedForwardMeteredDataMessageBuilder(
            messageId: MessageId.New(),
            senderId: ActorNumber.Create("5790001330552"),
            senderRole: ActorRole.DanishEnergyAgency,
            receiverId: ActorNumber.Create("1234567890123"),
            receiverRole: ActorRole.EnergySupplier,
            businessReason: BusinessReason.PeriodicFlexMetering,
            relatedToMessageId: MessageId.New(),
            originalTransactionIdReference: TransactionId.New(),
            transactionId: TransactionId.New(),
            timestamp: InstantPattern.General.Parse("2022-02-12T23:00:00Z").Value);

        rejectMessageBuilder.AddReasonToSeries(
            new RejectReason(
                ErrorCode: "E0I",
                ErrorMessage: "Error message 1"));
        rejectMessageBuilder.AddReasonToSeries(
            new RejectReason(
                ErrorCode: "A01",
                ErrorMessage: "Error message 2"));

        var marketDocumentStream = await CreateDocument(
            rejectMessageBuilder,
            DocumentFormat.FromName(documentFormat));

        await AssertAcknowledgementDocumentProvider.AssertDocument(marketDocumentStream.Stream, DocumentFormat.FromName(documentFormat))
            .HasMessageId(rejectMessageBuilder.MessageId)
            .HasSenderId(rejectMessageBuilder.SenderId)
            .HasSenderRole(rejectMessageBuilder.SenderRole)
            .HasReceiverId(rejectMessageBuilder.ReceiverId)
            .HasReceiverRole(rejectMessageBuilder.ReceiverRole)
            .HasCreationDate(rejectMessageBuilder.Timestamp)
            .HasRelatedToMessageId(rejectMessageBuilder.RelatedToMessageId)
            .HasReceivedBusinessReasonCode(rejectMessageBuilder.BusinessReason)

            .HasOriginalTransactionId(rejectMessageBuilder.OriginalTransactionIdReference)
            .HasTransactionId(rejectMessageBuilder.TransactionId) // Only ebix has this property
            .SeriesHasReasons(rejectMessageBuilder.GetSeries().RejectReasons.ToArray())
            .DocumentIsValidAsync();
    }

    private Task<MarketDocumentStream> CreateDocument(
        RejectedForwardMeteredDataMessageBuilder resultBuilder,
        DocumentFormat documentFormat)
    {
        var documentHeader = resultBuilder.BuildHeader();
        var records = _parser.From(resultBuilder.GetSeries());
        if (documentFormat == DocumentFormat.Ebix)
        {
            return new AcknowledgementEbixDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records });
        }

        if (documentFormat == DocumentFormat.Xml)
        {
            return new AcknowledgementXmlDocumentWriter(_parser).WriteAsync(
                documentHeader,
                new[] { records });
        }

        if (documentFormat == DocumentFormat.Json)
        {
            var serviceProvider = new ServiceCollection().AddJavaScriptEncoder().BuildServiceProvider();
            return new AcknowledgementJsonDocumentWriter(
                    _parser,
                    serviceProvider.GetRequiredService<JavaScriptEncoder>())
                .WriteAsync(documentHeader, new[] { records });
        }

        throw new Exception("No writer for the given format");
    }
}
