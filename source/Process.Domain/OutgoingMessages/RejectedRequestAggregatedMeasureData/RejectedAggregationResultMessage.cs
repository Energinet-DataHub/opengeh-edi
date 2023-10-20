﻿// Copyright 2020 Energinet DataHub A/S
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

namespace Energinet.DataHub.EDI.Process.Domain.OutgoingMessages.RejectedRequestAggregatedMeasureData;

public class RejectedAggregationResultMessage : OutgoingMessage
{
    public RejectedAggregationResultMessage(ActorNumber receiverId, ProcessId processId, string businessReason, MarketRole receiverRole, RejectedTimeSerie series)
        : base(DocumentType.RejectRequestAggregatedMeasureData, receiverId, processId, businessReason, receiverRole, DataHubDetails.IdentificationNumber, MarketRole.MeteringDataAdministrator, new Serializer().Serialize(series))
    {
        Series = series;
    }

    private RejectedAggregationResultMessage(DocumentType documentType, ActorNumber receiverId, ProcessId processId, string businessReason, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord)
        : base(documentType, receiverId, processId, businessReason, receiverRole, senderId, senderRole, messageRecord)
    {
        Series = new Serializer().Deserialize<RejectedTimeSerie>(messageRecord)!;
    }

    public RejectedTimeSerie Series { get; }
}

public record RejectedTimeSerie(
    Guid TransactionId,
    IReadOnlyList<RejectReason> RejectReasons,
    string OriginalTransactionIdReference);

public record RejectReason(string ErrorCode, string ErrorMessage);
