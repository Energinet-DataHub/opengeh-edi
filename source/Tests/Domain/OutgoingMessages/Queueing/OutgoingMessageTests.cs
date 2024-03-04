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

using BuildingBlocks.Application.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyWholesaleServices;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.RejectRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.Tests.Factories;
using FluentAssertions;
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
        var energyResultMessageDto = EnergyResultMessageDtoBuilder.Build();

        // Act
        var outgoingMesssage = OutgoingMessage.CreateMessage(
            energyResultMessageDto,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        var deserializedContent = serializer.Deserialize<TimeSeriesMarketActivityRecord>(outgoingMesssage.GetSerializedContent());
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
        var outgoingMesssage = OutgoingMessage.CreateMessage(
            acceptedEnergyResultMessageDto,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        var deserializedContent = serializer.Deserialize<TimeSeriesMarketActivityRecord>(outgoingMesssage.GetSerializedContent());
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
        var outgoingMesssage = OutgoingMessage.CreateMessage(
            rejectedEnergyResultMessageDto,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        var deserializedContent = serializer.Deserialize<RejectedTimeSerieMarketActivityRecord>(outgoingMesssage.GetSerializedContent());
        rejectedEnergyResultMessageDto.Series.Should().BeEquivalentTo(deserializedContent);
        rejectedEnergyResultMessageDto.Series.RejectReasons.Should().BeEquivalentTo(deserializedContent.RejectReasons);
    }

    [Fact]
    public void Ensure_wholesale_services_can_be_deserialized_to_market_activity_record()
    {
        // Arrange
        var serializer = new Serializer();
        var wholesaleServicesMessageDto = WholesaleServicesMessageDtoBuilder.Build();

        // Act
        var outgoingMesssages = OutgoingMessage.CreateMessages(
            wholesaleServicesMessageDto,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        outgoingMesssages.Should()
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
        var wholesaleServicesMessageDto = WholesaleServicesMessageDtoBuilder.Build();

        // Act
        var outgoingMesssages = OutgoingMessage.CreateMessages(
            wholesaleServicesMessageDto,
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        outgoingMesssages.Should().HaveCount(2);
    }
}
