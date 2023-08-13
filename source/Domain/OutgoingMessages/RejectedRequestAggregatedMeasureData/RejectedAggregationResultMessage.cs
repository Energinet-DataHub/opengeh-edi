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

using Domain.Actors;
using Domain.Documents;
using Domain.Transactions;

namespace Domain.OutgoingMessages.RejectedRequestAggregatedMeasureData;

public class RejectedAggregationResultMessage : OutgoingMessage
{
    public RejectedAggregationResultMessage(ActorNumber receiverId, TransactionId transactionId, string businessReason, MarketRole receiverRole, IReadOnlyList<RejectedTimeSerie> series)
        : base(DocumentType.RejectAggregatedMeasureData, receiverId, transactionId, businessReason, receiverRole, DataHubDetails.IdentificationNumber, MarketRole.MeteringDataAdministrator, new Serializer().Serialize(series))
    {
        Series = series;
    }

    private RejectedAggregationResultMessage(DocumentType documentType, ActorNumber receiverId, TransactionId transactionId, string businessReason, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord)
        : base(documentType, receiverId, transactionId, businessReason, receiverRole, senderId, senderRole, messageRecord)
    {
        Series = new Serializer().Deserialize<List<RejectedTimeSerie>>(messageRecord)!;
    }

    public IReadOnlyList<RejectedTimeSerie> Series { get; }
}

public record RejectedTimeSerie(
    Guid TransactionId,
    RejectReason RejectReason,
    string OriginalTransactionIdReference);

public record RejectReason(string ErrorCode, string ErrorMessage);
