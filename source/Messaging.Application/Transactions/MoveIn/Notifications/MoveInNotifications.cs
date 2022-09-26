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
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.OutgoingMessages.GenericNotification;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions.MoveIn;
using NodaTime;

namespace Messaging.Application.Transactions.MoveIn.Notifications;

public class MoveInNotifications
{
    private readonly IOutgoingMessageStore _outgoingMessageStore;
    private readonly IMarketActivityRecordParser _marketActivityRecordParser;

    public MoveInNotifications(IOutgoingMessageStore outgoingMessageStore, IMarketActivityRecordParser marketActivityRecordParser)
    {
        _outgoingMessageStore = outgoingMessageStore;
        _marketActivityRecordParser = marketActivityRecordParser;
    }

    public void InformCurrentEnergySupplierAboutEndOfSupply(string transactionId, Instant effectiveDate, string marketEvaluationPointId, string energySupplierId)
    {
        var marketActivityRecord = new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            transactionId,
            marketEvaluationPointId,
            effectiveDate);

        var message = new OutgoingMessage(
            DocumentType.GenericNotification,
            energySupplierId,
            transactionId,
            BusinessReasonCode.CustomerMoveInOrMoveOut.Code,
            MarketRoles.EnergySupplier,
            DataHubDetails.IdentificationNumber,
            MarketRoles.MeteringPointAdministrator,
            _marketActivityRecordParser.From(marketActivityRecord));

        _outgoingMessageStore.Add(message);
    }

    public void NotifyGridOperator(MoveInTransaction transaction, string gridOperatorNumber)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        var marketActivityRecord = new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            transaction.TransactionId,
            transaction.MarketEvaluationPointId,
            transaction.EffectiveDate);

        var message = new OutgoingMessage(
            DocumentType.GenericNotification,
            gridOperatorNumber,
            transaction.TransactionId,
            ProcessType.MoveIn.Code,
            MarketRoles.GridOperator,
            DataHubDetails.IdentificationNumber,
            MarketRoles.MeteringPointAdministrator,
            _marketActivityRecordParser.From(marketActivityRecord));

        _outgoingMessageStore.Add(message);
    }
}
