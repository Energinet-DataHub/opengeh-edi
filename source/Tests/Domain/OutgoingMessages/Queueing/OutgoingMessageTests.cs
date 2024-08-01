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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Application;
using Energinet.DataHub.EDI.OutgoingMessages.Domain;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyWholesaleServices;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RejectRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RejectRequestWholesaleSettlement;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.Tests.Factories;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Domain.OutgoingMessages.Queueing;

public class OutgoingMessageTests
{
    private readonly IList<IDocumentWriter> _documentWriters = new List<IDocumentWriter>()
    {
        new NotifyWholesaleServicesCimJsonDocumentWriter(new MessageRecordParser(new Serializer())),
        new NotifyWholesaleServicesEbixDocumentWriter(new MessageRecordParser(new Serializer())),
        new NotifyWholesaleServicesCimXmlDocumentWriter(new MessageRecordParser(new Serializer())),

        new NotifyAggregatedMeasureDataEbixDocumentWriter(new MessageRecordParser(new Serializer())),
        new NotifyAggregatedMeasureDataCimXmlDocumentWriter(new MessageRecordParser(new Serializer())),
        new NotifyAggregatedMeasureDataCimJsonDocumentWriter(new MessageRecordParser(new Serializer())),

        new RejectRequestAggregatedMeasureDataCimJsonDocumentWriter(new MessageRecordParser(new Serializer())),
        new RejectRequestAggregatedMeasureDataEbixDocumentWriter(new MessageRecordParser(new Serializer())),
        new RejectRequestAggregatedMeasureDataCimXmlDocumentWriter(new MessageRecordParser(new Serializer())),

        new RejectRequestWholesaleSettlementCimJsonDocumentWriter(new MessageRecordParser(new Serializer())),
        new RejectRequestWholesaleSettlementEbixDocumentWriter(new MessageRecordParser(new Serializer())),
        new RejectRequestWholesaleSettlementCimXmlDocumentWriter(new MessageRecordParser(new Serializer())),
    };

    /// <summary>
    /// This contains the serialized content for the different messages that we should be able to deserialize in order to write message from actor queues
    /// </summary>
    /// <remarks>NEVER REMOVE FROM THE LIST ONLY EXTEND IT, to ensure backwards compatibility</remarks>
    public static IEnumerable<object?[]> EnqueuedQueuedSerializedContents()
    {
        return new[]
        {
            new object?[] { DocumentType.RejectRequestAggregatedMeasureData, "{\"TransactionId\":\"4e85a732-85fd-4d92-8ff3-72c052802716\",\"RejectReasons\":[{\"ErrorCode\":\"E18\",\"ErrorMessage\":\"Det virker ikke!\"}],\"OriginalTransactionIdReference\":\"4E85A73285FD4D928FF372C052802717\"}" },
            new object?[] { DocumentType.NotifyAggregatedMeasureData, "{\"TransactionId\":\"b114111b-ee09-4a0a-8399-ddca6a7edeca\",\"GridAreaCode\":\"804\",\"MeteringPointType\":\"Consumption\",\"SettlementType\":\"Flex\",\"MeasureUnitType\":\"KWH\",\"Resolution\":\"QuarterHourly\",\"EnergySupplierNumber\":\"1234567890123\",\"BalanceResponsibleNumber\":\"1234567890124\",\"Period\":{\"Start\":\"2024-02-02T02:02:02Z\",\"End\":\"2024-02-02T02:02:02Z\"},\"Point\":[{\"Position\":1,\"Quantity\":2,\"QuantityQuality\":4,\"SampleTime\":\"2024-02-02T02:02:02Z\"}],\"CalculationResultVersion\":1,\"OriginalTransactionIdReference\":null,\"SettlementVersion\":\"FirstCorrection\"}" },
            new object?[] { DocumentType.NotifyWholesaleServices, "{\"TransactionId\":\"39843d03-d5e3-4695-b3a4-2d05a93373dc\",\"CalculationVersion\":1,\"GridAreaCode\":\"870\",\"ChargeCode\":\"123\",\"IsTax\":false,\"Points\":[{\"Position\":1,\"Quantity\":100,\"Price\":100,\"Amount\":100,\"QuantityQuality\":null}],\"EnergySupplier\":{\"Value\":\"1234567894444\"},\"ChargeOwner\":{\"Value\":\"1234567897777\"},\"Period\":{\"Start\":\"2023-11-01T00:00:00Z\",\"End\":\"2023-12-01T00:00:00Z\"},\"SettlementVersion\":null,\"QuantityMeasureUnit\":{\"Code\":\"KWH\",\"Name\":\"Kwh\"},\"QuantityUnit\":null,\"PriceMeasureUnit\":{\"Code\":\"KWH\",\"Name\":\"Kwh\"},\"Currency\":{\"Code\":\"DKK\",\"Name\":\"DanishCrowns\"},\"ChargeType\":{\"Code\":\"D02\",\"Name\":\"Fee\"},\"Resolution\":{\"Code\":\"P1M\",\"Name\":\"Monthly\"},\"MeteringPointType\":{\"Code\":\"E17\",\"Name\":\"Consumption\"},\"SettlementType\":{\"Code\":\"E02\",\"Name\":\"NonProfiled\"}}" },
            // Request Accepted
            new object?[] { DocumentType.NotifyAggregatedMeasureData, "{\"TransactionId\":\"2c928b8b-b596-43da-9dcb-e8a36748f415\",\"GridAreaCode\":\"804\",\"MeteringPointType\":\"Consumption\",\"SettlementType\":\"Flex\",\"MeasureUnitType\":\"KWH\",\"Resolution\":\"QuarterHourly\",\"EnergySupplierNumber\":\"1234567890123\",\"BalanceResponsibleNumber\":\"1234567890124\",\"Period\":{\"Start\":\"2024-02-02T02:02:02Z\",\"End\":\"2024-02-02T02:02:02Z\"},\"Point\":[{\"Position\":1,\"Quantity\":2,\"QuantityQuality\":4,\"SampleTime\":\"2024-02-02T02:02:02Z\"}],\"CalculationResultVersion\":1,\"OriginalTransactionIdReference\":\"643e50ea-8811-4ee2-81a7-5dac21731f22\",\"SettlementVersion\":\"FirstCorrection\"}" },
            new object?[] { DocumentType.NotifyWholesaleServices, "{\"OriginalTransactionIdReference\":\"941c025a-3c98-48e5-985d-d0c843c4dc49\",\"TransactionId\":\"050e2270-3702-4885-b021-15d22d3c0977\",\"CalculationVersion\":1,\"GridAreaCode\":\"870\",\"ChargeCode\":\"123\",\"IsTax\":false,\"Points\":[{\"Position\":1,\"Quantity\":100,\"Price\":100,\"Amount\":100,\"QuantityQuality\":null}],\"EnergySupplier\":{\"Value\":\"1234567894444\"},\"ChargeOwner\":{\"Value\":\"1234567897777\"},\"Period\":{\"Start\":\"2023-11-01T00:00:00Z\",\"End\":\"2023-12-01T00:00:00Z\"},\"SettlementVersion\":null,\"QuantityMeasureUnit\":{\"Code\":\"KWH\",\"Name\":\"Kwh\"},\"QuantityUnit\":null,\"PriceMeasureUnit\":{\"Code\":\"KWH\",\"Name\":\"Kwh\"},\"Currency\":{\"Code\":\"DKK\",\"Name\":\"DanishCrowns\"},\"ChargeType\":{\"Code\":\"D02\",\"Name\":\"Fee\"},\"Resolution\":{\"Code\":\"P1M\",\"Name\":\"Monthly\"},\"MeteringPointType\":{\"Code\":\"E17\",\"Name\":\"Consumption\"},\"SettlementType\":{\"Code\":\"E02\",\"Name\":\"NonProfiled\"}}" },
            new object?[] { DocumentType.RejectRequestWholesaleSettlement, "{\n  \"TransactionId\": \"4e85a732-85fd-4d92-8ff3-72c052802716\",\n  \"RejectReasons\": [\n    {\n      \"ErrorCode\": \"E18\",\n      \"ErrorMessage\": \"Det virker ikke!\"\n    }\n  ],\n  \"OriginalTransactionIdReference\": \"4E85A73285FD4D928FF372C052802717\"\n}" },

            // Delete this in the next PR
            new object?[] { DocumentType.NotifyAggregatedMeasureData, "{\"TransactionId\":\"2c928b8b-b596-43da-9dcb-e8a36748f415\",\"GridAreaCode\":\"804\",\"MeteringPointType\":\"Consumption\",\"SettlementType\":\"D01\",\"MeasureUnitType\":\"KWH\",\"Resolution\":\"QuarterHourly\",\"EnergySupplierNumber\":\"1234567890123\",\"BalanceResponsibleNumber\":\"1234567890124\",\"Period\":{\"Start\":\"2024-02-02T02:02:02Z\",\"End\":\"2024-02-02T02:02:02Z\"},\"Point\":[{\"Position\":1,\"Quantity\":2,\"QuantityQuality\":4,\"SampleTime\":\"2024-02-02T02:02:02Z\"}],\"CalculationResultVersion\":1,\"OriginalTransactionIdReference\":\"643e50ea-8811-4ee2-81a7-5dac21731f22\",\"SettlementVersion\":\"FirstCorrection\"}" },
        };
    }

    [Theory]
    [MemberData(nameof(EnqueuedQueuedSerializedContents))]
    public async Task Ensure_we_can_write_all_enqueued_messages(DocumentType documentType, string serializedContent)
    {
        // Arrange
        foreach (var documentWriter in _documentWriters)
        {
            if (documentWriter.HandlesType(documentType))
            {
                // Act
                var act = () => documentWriter.WriteAsync(GetHeader(), new List<string> { serializedContent });
                // Assert
                await act.Should().NotThrowAsync();
            }
        }
    }

    [Fact]
    public void Ensure_all_document_writer_is_represented()
    {
        // Arrange
        var documentWriters = GetAllDocumentWriters();

        // Act & Assert
        documentWriters.Should().AllSatisfy(x => _documentWriters.Should().Contain(d => d.GetType() == x));
    }

    [Fact]
    public void Ensure_energy_result_can_be_deserialized_to_market_activity_record()
    {
        // Arrange
        var serializer = new Serializer();
        var energyResultMessageDto = new EnergyResultPerGridAreaMessageDtoBuilder()
            .Build();

        // Act
        var outgoingMessage = OutgoingMessage.CreateMessage(
            energyResultMessageDto,
            serializer,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        var deserializedContent = serializer.Deserialize<TimeSeriesMarketActivityRecord>(outgoingMessage.GetSerializedContent());
        energyResultMessageDto.Series.Should().BeEquivalentTo(
            deserializedContent,
            "because the point should be the same. If one is changed, the other should be changed as well. Remember to add the enqueuedQueuedSerializedContents with the new serialized content");
        energyResultMessageDto.Series.Point.Should().BeEquivalentTo(
            deserializedContent.Point,
            "because the series point should be the same. If one is changed, the other should be changed as well. Remember to add the enqueuedQueuedSerializedContents with the new serialized content");
    }

    [Fact]
    public void Ensure_accepted_energy_result_can_be_deserialized_to_market_activity_record()
    {
        // Arrange
        var serializer = new Serializer();
        var acceptedEnergyResultMessageDtoBuilder = new AcceptedEnergyResultMessageDtoBuilder();
        var acceptedEnergyResultMessageDto = acceptedEnergyResultMessageDtoBuilder.Build();

        // Act
        var outgoingMessage = OutgoingMessage.CreateMessage(
            acceptedEnergyResultMessageDto,
            serializer,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        var deserializedContent = serializer.Deserialize<TimeSeriesMarketActivityRecord>(outgoingMessage.GetSerializedContent());
        acceptedEnergyResultMessageDto.Series.Should().BeEquivalentTo(
            deserializedContent,
            "because the series should be the same. If one is changed, the other should be changed as well. Remember to add the enqueuedQueuedSerializedContents with the new serialized content");
        acceptedEnergyResultMessageDto.Series.Point.Should().BeEquivalentTo(
            deserializedContent.Point,
            "because the series point should be the same. If one is changed, the other should be changed as well. Remember to add the enqueuedQueuedSerializedContents with the new serialized content");
    }

    [Fact]
    public void Ensure_rejected_energy_result_can_be_deserialized_to_market_activity_record()
    {
        // Arrange
        var serializer = new Serializer();
        var rejectedEnergyResultMessageDto = RejectedEnergyResultMessageDtoBuilder.Build();

        // Act
        var outgoingMessage = OutgoingMessage.CreateMessage(
            rejectedEnergyResultMessageDto,
            serializer,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        var deserializedContent = serializer.Deserialize<RejectedTimeSerieMarketActivityRecord>(outgoingMessage.GetSerializedContent());
        rejectedEnergyResultMessageDto.Series.Should().BeEquivalentTo(
            deserializedContent,
            "because the series should be the same. If one is changed, the other should be changed as well. Remember to add the enqueuedQueuedSerializedContents with the new serialized content");
        rejectedEnergyResultMessageDto.Series.RejectReasons.Should().BeEquivalentTo(
            deserializedContent.RejectReasons,
            "because the reject reasons should be the same. If one is changed, the other should be changed as well. Remember to add the enqueuedQueuedSerializedContents with the new serialized content");
    }

    [Fact]
    public void Ensure_wholesale_services_can_be_deserialized_to_market_activity_record()
    {
        // Arrange
        var serializer = new Serializer();
        var wholesaleServicesMessageDto = new WholesaleServicesMessageDtoBuilder()
            .Build();

        // Act
        var outgoingMessages = OutgoingMessage.CreateMessages(
            wholesaleServicesMessageDto,
            serializer,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        outgoingMessages.Should()
            .AllSatisfy(
                outgoingMesssage =>
                {
                    var deserializedContent =
                        serializer.Deserialize<WholesaleCalculationMarketActivityRecord>(
                            outgoingMesssage.GetSerializedContent());
                    wholesaleServicesMessageDto.Series.Should().BeEquivalentTo(
                        deserializedContent,
                        "because the series should be the same. If one is changed, the other should be changed as well. Remember to add the enqueuedQueuedSerializedContents with the new serialized content");
                    wholesaleServicesMessageDto.Series.Points.Should().BeEquivalentTo(
                        deserializedContent.Points,
                        "because the series points should be the same. If one is changed, the other should be changed as well. Remember to add the enqueuedQueuedSerializedContents with the new serialized content");
                });
    }

    [Fact]
    public void Ensure_accepted_wholesale_services_result_can_be_deserialized_to_market_activity_record()
    {
        // Arrange
        var serializer = new Serializer();
        var acceptedWholesaleServicesMessageDto = AcceptedWholesaleServicesMessageDtoBuilder.Build();

        // Act
        var outgoingMessage = OutgoingMessage.CreateMessage(
            acceptedWholesaleServicesMessageDto,
            serializer,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        var deserializedContent = serializer.Deserialize<WholesaleCalculationMarketActivityRecord>(outgoingMessage.GetSerializedContent());
        acceptedWholesaleServicesMessageDto.Series.Should().BeEquivalentTo(
            deserializedContent,
            "because the series should be the same. If one is changed, the other should be changed as well. Remember to add the enqueuedQueuedSerializedContents with the new serialized content");
        acceptedWholesaleServicesMessageDto.Series.Points.Should().BeEquivalentTo(
            deserializedContent.Points,
            "because the series points should be the same. If one is changed, the other should be changed as well. Remember to add the enqueuedQueuedSerializedContents with the new serialized content");
    }

    [Fact]
    public void Ensure_rejected_wholesale_services_can_be_deserialized_to_market_activity_record()
    {
        // Arrange
        var serializer = new Serializer();
        var rejectedWholesaleServicesMessageDto = RejectedWholesaleServicesMessageDtoBuilder.Build();

        // Act
        var outgoingMessage = OutgoingMessage.CreateMessage(
            rejectedWholesaleServicesMessageDto,
            serializer,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        var deserializedContent = serializer.Deserialize<RejectedWholesaleServicesRecord>(outgoingMessage.GetSerializedContent());
        rejectedWholesaleServicesMessageDto.Series.Should().BeEquivalentTo(
            deserializedContent,
            "the series should be the same. If one is changed, the other should be changed as well. Remember to add the enqueuedQueuedSerializedContents with the new serialized content");
        rejectedWholesaleServicesMessageDto.Series.RejectReasons.Should().BeEquivalentTo(
            deserializedContent.RejectReasons,
            "the reject reasons should be the same. If one is changed, the other should be changed as well. Remember to add the enqueuedQueuedSerializedContents with the new serialized content");
    }

    [Fact]
    public void Creates_two_outgoing_message_for_wholesale_services()
    {
        // Arrange
        var serializer = new Serializer();
        var wholesaleServicesMessageDto = new WholesaleServicesMessageDtoBuilder()
            .Build();

        // Act
        var outgoingMessages = OutgoingMessage.CreateMessages(
            wholesaleServicesMessageDto,
            serializer,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        outgoingMessages.Should().HaveCount(2);
    }

    /// <summary>
    /// This test verifies the "hack" for a MDR/GridOperator actor which is the same Actor but with two distinct roles MDR and GridOperator
    /// The actor uses the MDR (MeteredDataResponsible) role when making request (RequestAggregatedMeasureData)
    /// but uses the DDM (GridOperator) role when peeking.
    /// This means that a NotifyAggregatedMeasureData document with a MDR receiver should be added to the DDM ActorMessageQueue
    /// </summary>
    [Fact]
    public void ActorMessageQueueMetadata_is_DDM_when_document_is_NotifyAggregatedMeasureData_and_role_is_MDR()
    {
        // Arrange
        var energyResultMessageDto = new AcceptedEnergyResultMessageDtoBuilder()
            .WithReceiverRole(ActorRole.MeteredDataResponsible)
            .Build();

        // Act
        var outgoingMessage = OutgoingMessage.CreateMessage(
            energyResultMessageDto,
            new Serializer(),
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        using var scope = new AssertionScope();
        outgoingMessage.Receiver.ActorRole.Should().Be(ActorRole.MeteredDataResponsible);
        outgoingMessage.GetActorMessageQueueMetadata().ActorRole.Should().Be(ActorRole.GridOperator);
    }

    /// <summary>
    /// This test verifies the "hack" for a MDR/GridOperator actor which is the same Actor but with two distinct roles MDR and GridOperator
    /// doesn't apply for NotifyWholesaleServices (it should only apply for NotifyAggregatedMeasureData)
    /// </summary>
    [Fact]
    public void ActorMessageQueueMetadata_is_MDR_when_document_is_NotifyWholesaleServices_and_role_is_MDR()
    {
        // Arrange
        var energyResultMessageDto = new WholesaleServicesMessageDtoBuilder()
            .WithReceiverRole(ActorRole.MeteredDataResponsible)
            .Build();

        // Act
        var outgoingMessages = OutgoingMessage.CreateMessages(
            energyResultMessageDto,
            new Serializer(),
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        using var scope = new AssertionScope();
        outgoingMessages.Should().ContainSingle(m => m.Receiver.ActorRole == ActorRole.MeteredDataResponsible);
        outgoingMessages.Should().ContainSingle(m => m.GetActorMessageQueueMetadata().ActorRole == ActorRole.MeteredDataResponsible);
    }

    private static OutgoingMessageHeader GetHeader()
    {
        return new OutgoingMessageHeader(
            BusinessReason.BalanceFixing.Name,
            "1234567812345",
            ActorRole.MeteredDataAdministrator.Code,
            "1234567812345",
            ActorRole.DanishEnergyAgency.Code,
            MessageId.New().ToString()!,
            Instant.FromUtc(2022, 1, 1, 0, 0));
    }

    private static IEnumerable<Type> GetAllDocumentWriters()
    {
        return typeof(NotifyWholesaleServicesCimJsonDocumentWriter).Assembly.GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(IDocumentWriter)) && !t.IsAbstract);
    }
}
