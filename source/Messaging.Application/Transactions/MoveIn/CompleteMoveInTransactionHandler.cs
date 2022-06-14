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
using Messaging.Application.Common;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;

namespace Messaging.Application.Transactions.MoveIn;

public class CompleteMoveInTransactionHandler : IRequestHandler<CompleteMoveInTransaction, Unit>
{
    private readonly IMoveInTransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MoveInNotifications _notifications;

    public CompleteMoveInTransactionHandler(IMoveInTransactionRepository transactionRepository, IMarketActivityRecordParser marketActivityRecordParser, IOutgoingMessageStore outgoingMessageStore, IUnitOfWork unitOfWork, MoveInNotifications notifications)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
    }

    public async Task<Unit> Handle(CompleteMoveInTransaction request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var transaction = await _transactionRepository.GetByProcessIdAsync(request.ProcessId).ConfigureAwait(false);
        if (transaction is null)
        {
            throw new TransactionNotFoundException(request.ProcessId);
        }

        InformSupplierIfAny(transaction);

        await _unitOfWork.CommitAsync().ConfigureAwait(false);

        return Unit.Value;
    }

    private void InformSupplierIfAny(MoveInTransaction transaction)
    {
        if (transaction.CurrentEnergySupplierId is not null)
        {
            _notifications.InformCurrentEnergySupplierAboutEndOfSupply(transaction);
        }
    }
}
