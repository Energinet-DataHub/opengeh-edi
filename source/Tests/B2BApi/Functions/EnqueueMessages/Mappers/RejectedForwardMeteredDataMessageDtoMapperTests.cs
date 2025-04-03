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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021.Mappers;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Components.Abstractions.BusinessValidation;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Xunit;
using ActorNumber = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorNumber;
using ActorRole = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorRole;
using BusinessReason = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.BusinessReason;

namespace Energinet.DataHub.EDI.Tests.B2BApi.Functions.EnqueueMessages.Mappers;

public class RejectedForwardMeteredDataMessageDtoMapperTests
{
    [Fact]
    public void Given_ForwardMeteredDataRejectedV1_When_Mapping_Then_DtoHasExpectedValues()
    {
        // Arrange
        var serviceBusMessageId = Guid.NewGuid();
        var actor = new EnqueueActorMessagesActorV1
        {
            ActorNumber = "1231231231234", ActorRole = ActorRoleV1.Delegated,
        };

        var rejectData = new ForwardMeteredDataRejectedV1(
            OriginalActorMessageId: MessageId.New().Value,
            OriginalTransactionId: TransactionId.New().Value!,
            ForwardedByActorNumber: ActorNumber.Create("DeleteThisYes"),
            ForwardedByActorRole: ActorRole.EnergySupplier,
            ForwardedForActorRole: ActorRole.MeteredDataResponsible,
            BusinessReason: BusinessReason.PeriodicMetering,
            ValidationErrors: new List<ValidationErrorDto>()
            {
                new ValidationErrorDto(
                    ErrorCode: "E01",
                    Message: "Test error message"),
            });

        // Act
        var mappedValue = RejectedForwardMeteredDataMessageDtoMapper.Map(
            serviceBusMessageId,
            actor,
            rejectData);

        // Assert
        Assert.Equal(actor.ActorNumber, mappedValue.ReceiverNumber.Value);
        Assert.Equal(actor.ActorRole.ToString(), mappedValue.ReceiverRole.Name);

        Assert.Equal(serviceBusMessageId.ToString(), mappedValue.EventId.Value);
        Assert.Equal(serviceBusMessageId, mappedValue.ExternalId.Value);
        Assert.Equal(rejectData.BusinessReason.Name, mappedValue.BusinessReason);
        Assert.Equal(rejectData.ForwardedForActorRole, mappedValue.DocumentReceiverRole.ToProcessManagerActorRole());
        Assert.Equal(rejectData.OriginalActorMessageId, mappedValue.RelatedToMessageId?.Value);
        Assert.Equal(rejectData.OriginalTransactionId, mappedValue.Series.OriginalTransactionIdReference.Value);
        Assert.Collection(
            mappedValue.Series.RejectReasons,
            item =>
            {
                Assert.Equal(rejectData.ValidationErrors.First().ErrorCode, item.ErrorCode);
                Assert.Equal(rejectData.ValidationErrors.First().Message, item.ErrorMessage);
            });
    }
}
