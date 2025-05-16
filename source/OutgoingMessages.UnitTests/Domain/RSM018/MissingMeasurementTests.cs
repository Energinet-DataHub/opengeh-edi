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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM018;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MissingMeasurementMessages;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM018;

public class MissingMeasurementTests : IClassFixture<DocumentValidationFixture>
{
    private readonly MessageRecordParser _parser = new(new Serializer());

    [Theory]
    //[InlineData(nameof(DocumentFormat.Xml))]
    //[InlineData(nameof(DocumentFormat.Json))]
    [InlineData(nameof(DocumentFormat.Ebix))]
    public async Task Given_MissingMeasurement_When_CreateDocument_Then_DocumentCreated(string documentFormat)
    {
        var missingMeasurementBuilder = new MissingMeasurementMessageBuilder(
            messageId: MessageId.New(),
            senderId: ActorNumber.Create("5790001330552"),
            senderRole: ActorRole.DanishEnergyAgency,
            receiverId: ActorNumber.Create("1234567890123"),
            receiverRole: ActorRole.EnergySupplier,
            businessReason: BusinessReason.PeriodicFlexMetering,
            transactionId: TransactionId.New(),
            timestamp: InstantPattern.General.Parse("2022-02-13T23:00:00Z").Value);

        missingMeasurementBuilder.AddMissingMeasurement(
            new MissingMeasurement(
                TransactionId: TransactionId.New(),
                MeteringPointId: MeteringPointId.From("1234567890123"),
                Date: InstantPattern.General.Parse("2022-02-12T23:00:00Z").Value));

        missingMeasurementBuilder.AddMissingMeasurement(
            new MissingMeasurement(
                TransactionId: TransactionId.New(),
                MeteringPointId: MeteringPointId.From("1234567890123"),
                Date: InstantPattern.General.Parse("2022-02-13T23:00:00Z").Value));

        var marketDocumentStream = await CreateDocument(
            missingMeasurementBuilder,
            DocumentFormat.FromName(documentFormat));

        await AssertMissingMeasurementDocumentProvider.AssertDocument(
                marketDocumentStream.Stream,
                DocumentFormat.FromName(documentFormat))
            .HasMessageId(missingMeasurementBuilder.MessageId)
            .HasSenderId(missingMeasurementBuilder.SenderId)
            .HasSenderRole(missingMeasurementBuilder.SenderRole)
            .HasReceiverId(missingMeasurementBuilder.ReceiverId)
            .HasReceiverRole(missingMeasurementBuilder.ReceiverRole)
            .HasTimestamp(missingMeasurementBuilder.Timestamp)
            .HasBusinessReason(missingMeasurementBuilder.BusinessReason)
            .DocumentIsValidAsync();

        var series = missingMeasurementBuilder.GetSeries().ToList();
        for (int i = 1; i < series.Count + 1; i++)
        {
            var element = series[i - 1];
            AssertMissingMeasurementDocumentProvider.AssertDocument(
                    marketDocumentStream.Stream,
                    DocumentFormat.FromName(documentFormat))
                .HasTransactionId(i, element.TransactionId)
                .HasMeteringPointNumber(i, element.MeteringPointId)
                .HasMissingDate(i, element.Date);
        }
    }

    private Task<MarketDocumentStream> CreateDocument(
        MissingMeasurementMessageBuilder resultBuilder,
        DocumentFormat documentFormat)
    {
        var documentHeader = resultBuilder.BuildHeader();
        var records = resultBuilder.GetSeries().Select(_parser.From).ToList();

        if (documentFormat == DocumentFormat.Json)
        {
            var serviceProvider = new ServiceCollection().AddJavaScriptEncoder().BuildServiceProvider();
            return new MissingMeasurementJsonDocumentWriter(
                    _parser,
                    serviceProvider.GetRequiredService<JavaScriptEncoder>())
                .WriteAsync(documentHeader, records, CancellationToken.None);
        }

        if (documentFormat == DocumentFormat.Ebix)
        {
            return new MissingMeasurementEbixDocumentWriter(_parser)
                .WriteAsync(documentHeader, records);
        }

        throw new Exception("No writer for the given format");
    }
}
