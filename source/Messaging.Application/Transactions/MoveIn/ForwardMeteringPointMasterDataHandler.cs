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

namespace Messaging.Application.Transactions.MoveIn;

public class ForwardMeteringPointMasterDataHandler : IRequestHandler<ForwardMeteringPointMasterData, Unit>
{
    private readonly IMoveInTransactionRepository _transactionRepository;

    public ForwardMeteringPointMasterDataHandler(IMoveInTransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public Task<Unit> Handle(ForwardMeteringPointMasterData request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var transaction = _transactionRepository.GetById(request.TransactionId);
        if (transaction is null)
        {
            throw new MoveInException($"Could not find move in transaction '{request.TransactionId}'");
        }

        //TODO: Handle message creation and dispatching
        transaction.HasForwardedMeteringPointMasterData();
        return Task.FromResult(Unit.Value);
    }
}
