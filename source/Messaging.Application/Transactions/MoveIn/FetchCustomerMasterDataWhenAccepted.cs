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
using Messaging.Domain.Transactions.MoveIn.Events;

namespace Messaging.Application.Transactions.MoveIn;

public class FetchCustomerMasterDataWhenAccepted : INotificationHandler<MoveInWasAccepted>
{
    private readonly ICommandScheduler _commandScheduler;

    public FetchCustomerMasterDataWhenAccepted(ICommandScheduler commandScheduler)
    {
        _commandScheduler = commandScheduler ?? throw new ArgumentNullException(nameof(commandScheduler));
    }

    public Task Handle(MoveInWasAccepted notification, CancellationToken cancellationToken)
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));
        return _commandScheduler.EnqueueAsync(new FetchCustomerMasterData(
            notification.BusinessProcessId,
            notification.MarketEvaluationPointNumber,
            notification.TransactionId));
    }
}
