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

using Messaging.Domain.Actors;
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.MoveIn;
using NodaTime;

namespace Messaging.Domain.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;

public class CharacteristicsOfACustomerAtAnApMessage : OutgoingMessage
{
    private CharacteristicsOfACustomerAtAnApMessage(MessageType messageType, ActorNumber receiverId, TransactionId transactionId, string processType, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord)
        : base(messageType, receiverId, transactionId, processType, receiverRole, senderId, senderRole, messageRecord)
    {
        MarketActivityRecord =
            new Serializer().Deserialize<MarketActivityRecord>(
                messageRecord);
    }

    private CharacteristicsOfACustomerAtAnApMessage(MessageType messageType, ActorNumber receiverId, TransactionId transactionId, string processType, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, MarketActivityRecord marketActivityRecord)
        : base(messageType, receiverId, transactionId, processType, receiverRole, senderId, senderRole, new Serializer().Serialize(marketActivityRecord))
    {
        MarketActivityRecord = marketActivityRecord;
    }

    public MarketActivityRecord MarketActivityRecord { get; }

    public static CharacteristicsOfACustomerAtAnApMessage Create(
        string transactionId,
        ProcessType processType,
        ActorNumber actorNumber,
        MarketRole receivingActorRole,
        Instant validityStart,
        CustomerMasterData customerMasterData)
    {
        ArgumentNullException.ThrowIfNull(processType);
        ArgumentNullException.ThrowIfNull(customerMasterData);

        var marketEvaluationPoint = CreateMarketEvaluationPoint(customerMasterData);
        var marketActivityRecord = new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            transactionId,
            validityStart,
            marketEvaluationPoint);

        return new CharacteristicsOfACustomerAtAnApMessage(
            MessageType.CharacteristicsOfACustomerAtAnAP,
            actorNumber,
            TransactionId.Create(transactionId),
            processType.Code,
            receivingActorRole,
            DataHubDetails.IdentificationNumber,
            MarketRole.MeteringPointAdministrator,
            marketActivityRecord);
    }

    private static MarketEvaluationPoint CreateMarketEvaluationPoint(CustomerMasterData masterData)
    {
        return new MarketEvaluationPoint(
            masterData.MarketEvaluationPoint,
            masterData.ElectricalHeating,
            masterData.ElectricalHeatingStart,
            new MrId(masterData.FirstCustomerId, "ARR"),
            masterData.FirstCustomerName,
            masterData.SecondCustomerId is null ? null : new MrId(masterData.SecondCustomerId, "ARR"),
            masterData.SecondCustomerName,
            masterData.ProtectedName,
            masterData.HasEnergySupplier,
            masterData.SupplyStart,
            new List<UsagePointLocation>()
            {
                CreateEmptyUsagePointLocation("D01"),
                CreateEmptyUsagePointLocation("D04"),
            });
    }

    private static MainAddress CreateEmptyAddress()
    {
        return new MainAddress(
            new StreetDetail(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
            new TownDetail(string.Empty, string.Empty, string.Empty, string.Empty),
            string.Empty,
            string.Empty);
    }

    private static UsagePointLocation CreateEmptyUsagePointLocation(string type)
    {
        return new UsagePointLocation(
            type,
            string.Empty,
            CreateEmptyAddress(),
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            false);
    }
}
