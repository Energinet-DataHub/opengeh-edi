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

using System;
using System.Threading.Tasks;
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions.MoveIn;

namespace Messaging.Application.Transactions.MoveIn.MasterDataDelivery;

public class CustomerMasterDataMessageFactory
{
    private readonly IMarketActivityRecordParser _marketActivityRecordParser;

    public CustomerMasterDataMessageFactory(IMarketActivityRecordParser marketActivityRecordParser)
    {
        _marketActivityRecordParser = marketActivityRecordParser;
    }

    public Task<OutgoingMessage> CreateFromAsync(MoveInTransaction transaction, ActorNumber receiverNumber, MarketRole receiverRole)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(transaction.CustomerMasterData);
        ArgumentNullException.ThrowIfNull(receiverNumber);

        var marketActivityRecord = CreateMarketActivityRecord(transaction);

        return Task.FromResult(CreateOutgoingMessage(
            transaction.StartedByMessageId,
            ProcessType.MoveIn.Code,
            receiverNumber.Value,
            receiverRole,
            _marketActivityRecordParser.From(marketActivityRecord)));
    }

    private static MarketActivityRecord CreateMarketActivityRecord(MoveInTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(transaction.CustomerMasterData);

        var marketEvaluationPoint = CreateMarketEvaluationPoint(transaction.CustomerMasterData);
        var marketActivityRecord = new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            transaction.TransactionId,
            transaction.EffectiveDate,
            marketEvaluationPoint);
        return marketActivityRecord;
    }

    private static OutgoingMessage CreateOutgoingMessage(string id, string processType, string receiverId, MarketRole receiverRole, string @marketActivityRecordPayload)
    {
        return new OutgoingMessage(
            DocumentType.CharacteristicsOfACustomerAtAnAP,
            ActorNumber.Create(receiverId),
            id,
            processType,
            receiverRole,
            DataHubDetails.IdentificationNumber,
            MarketRole.MeteringPointAdministrator,
            marketActivityRecordPayload);
    }

    private static MarketEvaluationPoint CreateMarketEvaluationPoint(CustomerMasterData masterData)
    {
        return new MarketEvaluationPoint(
            masterData.MarketEvaluationPoint,
            masterData.ElectricalHeating,
            masterData.ElectricalHeatingStart,
            new MrId(masterData.FirstCustomerId, "ARR"),
            masterData.FirstCustomerName,
            new MrId(masterData.SecondCustomerId, "ARR"),
            masterData.SecondCustomerName,
            masterData.ProtectedName,
            masterData.HasEnergySupplier,
            masterData.SupplyStart,
            Array.Empty<UsagePointLocation>());
    }
}
