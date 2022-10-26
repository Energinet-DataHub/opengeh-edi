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

using System.Text.Json;
using Messaging.Domain.Actors;

namespace Messaging.Domain.OutgoingMessages.RejectRequestChangeOfSupplier;

public class RejectRequestChangeOfSupplierMessage : OutgoingMessage
{
    public RejectRequestChangeOfSupplierMessage(DocumentType documentType, ActorNumber receiverId, string transactionId, string processType, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string marketActivityRecordPayload)
        : base(documentType, receiverId, transactionId, processType, receiverRole, senderId, senderRole, marketActivityRecordPayload)
    {
        ArgumentNullException.ThrowIfNull(marketActivityRecordPayload);
        MarketActivityRecord = JsonSerializer.Deserialize<MarketActivityRecord>(marketActivityRecordPayload)!;
    }

    public RejectRequestChangeOfSupplierMessage(ActorNumber receiverId, string transactionId, string processType, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, MarketActivityRecord marketActivityRecord)
        : base(DocumentType.RejectRequestChangeOfSupplier, receiverId, transactionId, processType, receiverRole, senderId, senderRole, JsonSerializer.Serialize(marketActivityRecord))
    {
        MarketActivityRecord = marketActivityRecord;
    }

    public MarketActivityRecord MarketActivityRecord { get; }

    public static RejectRequestChangeOfSupplierMessage Create(
        string transactionId,
        ProcessType processType,
        string marketEvaluationPointNumber,
        ActorNumber energySupplierNumber,
        IReadOnlyList<Reason> reasons)
    {
        ArgumentNullException.ThrowIfNull(processType);
        ArgumentNullException.ThrowIfNull(energySupplierNumber);
        ArgumentNullException.ThrowIfNull(reasons);

        if (reasons.Count == 0)
        {
            throw new OutgoingMessageException($"Reject message must contain at least one reject reason");
        }

        var marketActivityRecord = new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            transactionId,
            marketEvaluationPointNumber,
            reasons);
        return new RejectRequestChangeOfSupplierMessage(
            energySupplierNumber,
            transactionId,
            processType.Code,
            MarketRole.EnergySupplier,
            DataHubDetails.IdentificationNumber,
            MarketRole.MeteringPointAdministrator,
            marketActivityRecord);
    }
}
