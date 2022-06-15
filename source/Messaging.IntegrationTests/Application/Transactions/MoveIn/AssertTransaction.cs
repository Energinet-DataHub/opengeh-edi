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
using System.Runtime.CompilerServices;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.Transactions.MoveIn;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class AssertTransaction
{
    private readonly dynamic _transaction;

    private AssertTransaction(dynamic transaction)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        Assert.NotNull(transaction);
        _transaction = transaction;
    }

    public static AssertTransaction Transaction(string transactionId, IDbConnectionFactory connectionFactory)
    {
        if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));

        var transaction = connectionFactory.GetOpenConnection().QuerySingle(
            $"SELECT * FROM b2b.MoveInTransactions WHERE TransactionId = @TransactionId",
            new
            {
                TransactionId = transactionId,
            });

        Assert.NotNull(transaction);
        return new AssertTransaction(transaction);
    }

    public AssertTransaction WithState(MoveInTransaction.State expectedState)
    {
        Assert.Equal(expectedState.ToString(), _transaction.State);
        return this;
    }
}
