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
using Energinet.DataHub.EDI.Application.OutgoingMessages;
using Energinet.DataHub.EDI.Application.OutgoingMessages.Common;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.OutgoingMessages.MoveIn.GenericNotification;
using Energinet.DataHub.EDI.Domain.Transactions.MoveIn;

namespace Energinet.DataHub.EDI.Application.Transactions.MoveIn.Notifications;

public class MoveInNotifications
{
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly IMessageRecordParser _messageRecordParser;

    public MoveInNotifications(IOutgoingMessageRepository outgoingMessageRepository, IMessageRecordParser messageRecordParser)
    {
        _outgoingMessageRepository = outgoingMessageRepository;
        _messageRecordParser = messageRecordParser;
    }

    public void InformCurrentEnergySupplierAboutEndOfSupply(MoveInTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var marketActivityRecord = new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            transaction.ActorProvidedId.Id,
            transaction.MarketEvaluationPointId,
            transaction.EffectiveDate);

        var message = new OutgoingMessage(
            DocumentType.GenericNotification,
            ActorNumber.Create(transaction.CurrentEnergySupplierId!),
            transaction.ProcessId,
            BusinessReason.MoveIn.Name,
            MarketRole.EnergySupplier,
            DataHubDetails.IdentificationNumber,
            MarketRole.MeteringPointAdministrator,
            _messageRecordParser.From(marketActivityRecord));

        _outgoingMessageRepository.Add(message);
    }

    public void NotifyGridOperator(MoveInTransaction transaction, string gridOperatorNumber)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        var marketActivityRecord = new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            transaction.ActorProvidedId.Id,
            transaction.MarketEvaluationPointId,
            transaction.EffectiveDate);

        var message = new OutgoingMessage(
            DocumentType.GenericNotification,
            ActorNumber.Create(gridOperatorNumber),
            transaction.ProcessId,
            BusinessReason.MoveIn.Name,
            MarketRole.GridOperator,
            DataHubDetails.IdentificationNumber,
            MarketRole.MeteringPointAdministrator,
            _messageRecordParser.From(marketActivityRecord));

        _outgoingMessageRepository.Add(message);
    }
}
