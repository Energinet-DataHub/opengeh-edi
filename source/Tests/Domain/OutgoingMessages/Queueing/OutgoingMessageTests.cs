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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyWholesaleServices;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.RejectRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.Tests.Factories;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Domain.OutgoingMessages.Queueing;

public class OutgoingMessageTests
{
    [Fact]
    public void Ensure_energy_result_can_be_deserialized_to_market_activity_record()
    {
        // Arrange
        var serializer = new Serializer();
        var energyResultMessageDto = new EnergyResultMessageDtoBuilder()
            .Build();

        // Act
        var outgoingMessage = OutgoingMessage.CreateMessage(
            energyResultMessageDto,
            serializer,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        var deserializedContent = serializer.Deserialize<TimeSeriesMarketActivityRecord>(outgoingMessage.GetSerializedContent());
        energyResultMessageDto.Series.Should().BeEquivalentTo(deserializedContent);
        energyResultMessageDto.Series.Point.Should().BeEquivalentTo(deserializedContent.Point);
    }

    [Fact]
    public void Ensure_accepted_energy_result_can_be_deserialized_to_market_activity_record()
    {
        // Arrange
        var serializer = new Serializer();
        var acceptedEnergyResultMessageDto = AcceptedEnergyResultMessageDtoBuilder.Build();

        // Act
        var outgoingMessage = OutgoingMessage.CreateMessage(
            acceptedEnergyResultMessageDto,
            serializer,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        var deserializedContent = serializer.Deserialize<TimeSeriesMarketActivityRecord>(outgoingMessage.GetSerializedContent());
        acceptedEnergyResultMessageDto.Series.Should().BeEquivalentTo(deserializedContent);
        acceptedEnergyResultMessageDto.Series.Point.Should().BeEquivalentTo(deserializedContent.Point);
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
        rejectedEnergyResultMessageDto.Series.Should().BeEquivalentTo(deserializedContent);
        rejectedEnergyResultMessageDto.Series.RejectReasons.Should().BeEquivalentTo(deserializedContent.RejectReasons);
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
                    wholesaleServicesMessageDto.Series.Should().BeEquivalentTo(deserializedContent);
                    wholesaleServicesMessageDto.Series.Points.Should().BeEquivalentTo(deserializedContent.Points);
                });
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
        var energyResultMessageDto = new EnergyResultMessageDtoBuilder()
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
}
