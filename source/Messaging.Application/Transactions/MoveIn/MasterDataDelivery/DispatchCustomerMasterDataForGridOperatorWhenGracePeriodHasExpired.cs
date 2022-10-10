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
using Dapper;
using MediatR;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.Commands;
using Messaging.Application.Configuration.DataAccess;

namespace Messaging.Application.Transactions.MoveIn.MasterDataDelivery;

public class DispatchCustomerMasterDataForGridOperatorWhenGracePeriodHasExpired : INotificationHandler<ADayHasPassed>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly CommandSchedulerFacade _commandScheduler;

    public DispatchCustomerMasterDataForGridOperatorWhenGracePeriodHasExpired(
        IDbConnectionFactory connectionFactory,
        CommandSchedulerFacade commandScheduler)
    {
        _connectionFactory = connectionFactory;
        _commandScheduler = commandScheduler;
    }

    public async Task Handle(ADayHasPassed notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var sql =
            @"SELECT TransactionId FROM [b2b].[MoveInTransactions] WHERE GridOperator_MessageDeliveryState_CustomerMasterData = 'Pending' " +
            "AND CustomerMasterData IS NOT NULL AND DATEDIFF(day, EffectiveDate, @Now) >= 1";

        var transactionIds = await _connectionFactory.GetOpenConnection().QueryAsync<string>(
            sql,
            new { Now = notification.Now.ToDateTimeUtc(), })
            .ConfigureAwait(false);

        foreach (var transactionId in transactionIds)
        {
            await _commandScheduler.EnqueueAsync(new SendCustomerMasterDataToGridOperator(transactionId))
                .ConfigureAwait(false);
        }
    }
}
