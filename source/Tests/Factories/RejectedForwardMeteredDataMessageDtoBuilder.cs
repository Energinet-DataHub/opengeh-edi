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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.Tests.Factories;

public class RejectedForwardMeteredDataMessageBuilder
{
    private static readonly EventId _eventId = EventId.From(Guid.NewGuid());
    private static readonly ExternalId _externalId = new ExternalId(Guid.NewGuid());
    private static readonly BusinessReason _businessReason = BusinessReason.PeriodicFlexMetering;
    private static readonly ActorNumber _receiverId = ActorNumber.Create("1234567890123");
    private static readonly ActorRole _receiverRole = ActorRole.EnergySupplier;
    private static readonly MessageId _relatedToMessageId = MessageId.New();
    private static readonly TransactionId _transactionId = TransactionId.New();
    private static readonly TransactionId _originalTransactionIdReference = TransactionId.New();
    private static readonly List<RejectReason> _rejectReasons = new List<RejectReason>();

    private static readonly RejectedForwardMeteredDataSeries _series = new RejectedForwardMeteredDataSeries(
        TransactionId: _transactionId,
        OriginalTransactionIdReference: _originalTransactionIdReference,
#pragma warning disable SA1118
        RejectReasons: _rejectReasons.Count != 0
            ? _rejectReasons
            : new List<RejectReason>
            {
                new RejectReason(
                    ErrorCode: "E01",
                    ErrorMessage: "An error has occurred"),
            });
#pragma warning restore SA1118

    public static RejectedForwardMeteredDataMessageDto BuildDto()
    {
        return new RejectedForwardMeteredDataMessageDto(
            eventId: _eventId,
            externalId: _externalId,
            businessReason: _businessReason,
            receiverId: _receiverId,
            receiverRole: _receiverRole,
            relatedToMessageId: _relatedToMessageId,
            series: _series);
    }

    public OutgoingMessageHeader BuildHeader()
    {
        return new OutgoingMessageHeader(
            _businessReason.Name,
            "5790001330552",
            ActorRole.DanishEnergyAgency.Code,
            _receiverId.Value,
            _receiverRole.Code,
            MessageId: MessageId.New().Value,
            InstantPattern.General.Parse("2022-02-12T23:00:00Z").Value);
    }

    public RejectedForwardMeteredDataSeries GetSeries()
    {
        return _series;
    }
}
