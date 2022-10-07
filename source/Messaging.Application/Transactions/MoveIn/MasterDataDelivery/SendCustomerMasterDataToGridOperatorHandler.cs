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
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Domain.Actors;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions.MoveIn;
using MarketEvaluationPoint = Messaging.Application.OutgoingMessages.CharacteristicsOfACustomerAtAnAp.MarketEvaluationPoint;

namespace Messaging.Application.Transactions.MoveIn.MasterDataDelivery;

public class SendCustomerMasterDataToGridOperatorHandler : IRequestHandler<SendCustomerMasterDataToGridOperator, Unit>
{
    private readonly IMoveInTransactionRepository _transactionRepository;
    private readonly IOutgoingMessageStore _outgoingMessageStore;
    private readonly IMarketActivityRecordParser _marketActivityRecordParser;
    private readonly IMarketEvaluationPointRepository _marketEvaluationPointRepository;
    private readonly IActorLookup _actorLookup;

    public SendCustomerMasterDataToGridOperatorHandler(
        IMoveInTransactionRepository transactionRepository,
        IOutgoingMessageStore outgoingMessageStore,
        IMarketActivityRecordParser marketActivityRecordParser,
        IMarketEvaluationPointRepository marketEvaluationPointRepository,
        IActorLookup actorLookup)
    {
        _transactionRepository = transactionRepository;
        _outgoingMessageStore = outgoingMessageStore;
        _marketActivityRecordParser = marketActivityRecordParser;
        _marketEvaluationPointRepository = marketEvaluationPointRepository;
        _actorLookup = actorLookup;
    }

    public async Task<Unit> Handle(SendCustomerMasterDataToGridOperator request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transaction = _transactionRepository.GetById(request.TransactionId);
        if (transaction is null)
        {
            throw TransactionNotFoundException.TransactionIdNotFound(request.TransactionId);
        }

        _outgoingMessageStore.Add(
            await CustomerCharacteristicsMessageFromAsync(transaction.CustomerMasterData!, transaction).ConfigureAwait(false));
        transaction.SetCustomerMasterDataDeliveredWasToGridOperator();

        return Unit.Value;
    }

    private static OutgoingMessage CreateOutgoingMessage(string id, string processType, string receiverId, string @marketActivityRecordPayload)
    {
        return new OutgoingMessage(
            DocumentType.CharacteristicsOfACustomerAtAnAP,
            ActorNumber.Create(receiverId),
            id,
            processType,
            MarketRole.GridOperator,
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

    private async Task<OutgoingMessage> CustomerCharacteristicsMessageFromAsync(CustomerMasterData requestMasterDataContent, MoveInTransaction transaction)
    {
        var marketEvaluationPoint = CreateMarketEvaluationPoint(requestMasterDataContent);
        var marketActivityRecord = new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            transaction.TransactionId,
            transaction.EffectiveDate,
            marketEvaluationPoint);

        var gridOperatorNumber =
            await GetGridOperatorNumberAsync(transaction.MarketEvaluationPointId).ConfigureAwait(false);

        return CreateOutgoingMessage(
            transaction.StartedByMessageId,
            ProcessType.MoveIn.Code,
            gridOperatorNumber.Value,
            _marketActivityRecordParser.From(marketActivityRecord));
    }

    private async Task<ActorNumber> GetGridOperatorNumberAsync(string marketEvaluationPointNumber)
    {
        var marketEvaluationPoint = await _marketEvaluationPointRepository
            .GetByNumberAsync(marketEvaluationPointNumber).ConfigureAwait(false);
        return ActorNumber.Create(await _actorLookup.GetActorNumberByIdAsync(marketEvaluationPoint!.GridOperatorId.GetValueOrDefault())
            .ConfigureAwait(false));
    }
}
