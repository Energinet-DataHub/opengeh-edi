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
using Messaging.Application.OutgoingMessages;
using Messaging.Domain.Actors;
using Messaging.Domain.Transactions.MoveIn;

namespace Messaging.Application.Transactions.MoveIn.MasterDataDelivery;

public class SendCustomerMasterDataToEnergySupplierHandler : IRequestHandler<SendCustomerMasterDataToEnergySupplier, Unit>
{
    private readonly IMoveInTransactionRepository _transactionRepository;
    private readonly IOutgoingMessageStore _outgoingMessageStore;
    private readonly CustomerMasterDataMessageFactory _messageFactory;

    public SendCustomerMasterDataToEnergySupplierHandler(
        IMoveInTransactionRepository transactionRepository,
        IOutgoingMessageStore outgoingMessageStore,
        CustomerMasterDataMessageFactory messageFactory)
    {
        _transactionRepository = transactionRepository;
        _outgoingMessageStore = outgoingMessageStore;
        _messageFactory = messageFactory;
    }

    public async Task<Unit> Handle(SendCustomerMasterDataToEnergySupplier request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var transaction = _transactionRepository.GetById(request.TransactionId);
        if (transaction is null)
        {
            throw new MoveInException($"Could not find move in transaction '{request.TransactionId}'");
        }

        _outgoingMessageStore.Add(
            await _messageFactory.CreateFromAsync(transaction, ActorNumber.Create(transaction.NewEnergySupplierId), MarketRole.EnergySupplier)
                .ConfigureAwait(false));
        transaction.MarkCustomerMasterDataAsSent();

        return await Task.FromResult(Unit.Value).ConfigureAwait(false);
    }
}
