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
using Messaging.Application.MasterData;
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.MoveIn;

namespace Messaging.Application.Transactions.MoveIn.MasterDataDelivery;

public class SetCurrentKnownCustomerMasterDataHandler : IRequestHandler<SetCurrentKnownCustomerMasterData, Unit>
{
    private readonly IMoveInTransactionRepository _transactionRepository;

    public SetCurrentKnownCustomerMasterDataHandler(IMoveInTransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public Task<Unit> Handle(SetCurrentKnownCustomerMasterData request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var transaction = _transactionRepository.GetById(TransactionId.Create(request.TransactionId));
        if (transaction is null)
        {
            throw TransactionNotFoundException.TransactionIdNotFound(request.TransactionId);
        }

        transaction.SetCurrentKnownCustomerMasterData(ParseFrom(request.Data));
        return Task.FromResult(Unit.Value);
    }

    private static CustomerMasterData ParseFrom(CustomerMasterDataContent data)
    {
        return new CustomerMasterData(
            marketEvaluationPoint: data.MarketEvaluationPoint,
            electricalHeating: data.ElectricalHeating,
            electricalHeatingStart: data.ElectricalHeatingStart,
            firstCustomerId: data.FirstCustomerId,
            firstCustomerName: data.FirstCustomerName,
            secondCustomerId: data.SecondCustomerId,
            secondCustomerName: data.SecondCustomerName,
            protectedName: data.ProtectedName,
            hasEnergySupplier: data.HasEnergySupplier,
            supplyStart: data.SupplyStart);
    }
}
