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
using Energinet.DataHub.EDI.OutgoingMessages.Application;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Factories;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Application;

public class OutgoingMessageFactoryTests
{
    [Fact]
    public void Test213()
    {
        // Arrange
        var currentInstant = Instant.FromUtc(2024, 04, 02, 0, 0);
        var reject = new RejectedForwardMeteredDataMessageDtoBuilder()
            .Build();

        // Act
        var outgoingMessage = OutgoingMessageFactory.CreateMessage(
            reject,
            new Serializer(),
            currentInstant);

        // Assert
        Assert.Equal(reject.EventId, outgoingMessage.EventId);
        Assert.Equal(reject.DocumentType, outgoingMessage.DocumentType);
        Assert.Equal(reject.ExternalId, outgoingMessage.ExternalId);
        Assert.Equal(reject.BusinessReason, outgoingMessage.BusinessReason);
        Assert.Equal(reject.ReceiverNumber, outgoingMessage.Receiver.Number);
        Assert.Equal(reject.ReceiverRole, outgoingMessage.Receiver.ActorRole);
        Assert.Equal(reject.DocumentReceiverRole, outgoingMessage.DocumentReceiver.ActorRole);
        Assert.Equal(reject.ReceiverNumber, outgoingMessage.DocumentReceiver.Number);
        Assert.Equal(reject.RelatedToMessageId, outgoingMessage.RelatedToMessageId);
        Assert.Equal(currentInstant, outgoingMessage.CreatedAt);
        Assert.Equal(ProcessType.OutgoingMeteredDataForMeteringPoint, outgoingMessage.MessageCreatedFromProcess);
        Assert.Null(outgoingMessage.GridAreaCode);
        Assert.Null(outgoingMessage.CalculationId);
        Assert.Null(outgoingMessage.PeriodStartedAt);
    }
}
