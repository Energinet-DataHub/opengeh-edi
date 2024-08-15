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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RejectRequestAggregatedMeasureData;

namespace Energinet.DataHub.EDI.Tests.Factories;

public static class RejectedEnergyResultMessageDtoBuilder
{
    private static readonly ActorNumber _receiverNumber = ActorNumber.Create("1234567890123");
    private static readonly Guid _processId = Guid.NewGuid();
    private static readonly string _businessReason = BusinessReason.BalanceFixing.Code;
    private static readonly ActorRole _receiverRole = ActorRole.MeteredDataResponsible;
    private static readonly MessageId _relatedToMessageId = MessageId.Create(Guid.NewGuid().ToString());

    private static readonly RejectedEnergyResultMessageSerie _series = new(
        SampleData.TransactionId,
        new List<RejectedEnergyResultMessageRejectReason>
        {
            new(SampleData.SerieReasonCode, SampleData.SerieReasonMessage),
        },
        SampleData.OriginalTransactionId);

    private static readonly EventId _eventId = EventId.From(Guid.NewGuid().ToString());

    public static RejectedEnergyResultMessageDto Build()
    {
        return new RejectedEnergyResultMessageDto(
            _receiverNumber,
            _processId,
            _eventId,
            _businessReason,
            _receiverRole,
            _relatedToMessageId,
            _series,
            _receiverNumber,
            _receiverRole);
    }
}
