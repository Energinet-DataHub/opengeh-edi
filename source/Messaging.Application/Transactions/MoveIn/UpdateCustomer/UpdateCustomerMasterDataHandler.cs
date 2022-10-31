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
using Messaging.Domain.Transactions.MoveIn;

namespace Messaging.Application.Transactions.MoveIn.UpdateCustomer;

public class UpdateCustomerMasterDataHandler : IRequestHandler<UpdateCustomerMasterData, Unit>
{
    private readonly IMoveInTransactionRepository _transactionRepository;
    private readonly IUpdateCustomerMasterDataRequestClient _updateCustomerMasterDataRequestClient;

    public UpdateCustomerMasterDataHandler(IUpdateCustomerMasterDataRequestClient updateCustomerMasterDataRequestClient, IMoveInTransactionRepository transactionRepository)
    {
        _updateCustomerMasterDataRequestClient = updateCustomerMasterDataRequestClient;
        _transactionRepository = transactionRepository;
    }

    public async Task<Unit> Handle(UpdateCustomerMasterData request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transaction = await _transactionRepository.GetByEffectiveDateAsync().ConfigureAwait(false);
        if (transaction is null)
        {
            throw TransactionNotFoundException.TransactionIdNotFound(request.TransactionId);
        }

        await _updateCustomerMasterDataRequestClient.SendRequestAsync().ConfigureAwait(false);
        return Unit.Value;
    }
}
