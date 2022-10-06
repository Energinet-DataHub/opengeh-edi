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
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.Serialization;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class AssertTransaction
{
    private readonly dynamic _transaction;
    private readonly ISerializer? _serializer;

    private AssertTransaction(dynamic transaction)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        Assert.NotNull(transaction);
        _transaction = transaction;
    }

    private AssertTransaction(dynamic transaction, ISerializer serializer)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        Assert.NotNull(transaction);
        _transaction = transaction;
        _serializer = serializer;
    }

    public static AssertTransaction Transaction(string transactionId, IDbConnectionFactory connectionFactory)
    {
        if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));
        return new AssertTransaction(GetTransaction(transactionId, connectionFactory));
    }

    public static AssertTransaction Transaction(string transactionId, IDbConnectionFactory connectionFactory, ISerializer serializer)
    {
        if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));

        var transaction = connectionFactory.GetOpenConnection().QuerySingle(
            $"SELECT * FROM b2b.MoveInTransactions WHERE TransactionId = @TransactionId",
            new
            {
                TransactionId = transactionId,
            });

        Assert.NotNull(transaction);
        return new AssertTransaction(transaction, serializer);
    }

    public AssertTransaction HasState(MoveInTransaction.State expectedState)
    {
        Assert.Equal(expectedState.ToString(), _transaction.State);
        return this;
    }

    public AssertTransaction HasProcessId(string expectedProcessId)
    {
        Assert.Equal(expectedProcessId, _transaction.ProcessId);
        return this;
    }

    public AssertTransaction HasStartedByMessageId(string startedByMessageId)
    {
        Assert.Equal(startedByMessageId, _transaction.StartedByMessageId);
        return this;
    }

    public AssertTransaction HasNewEnergySupplierId(string newEnergySupplierId)
    {
        Assert.Equal(newEnergySupplierId, _transaction.NewEnergySupplierId);
        return this;
    }

    public AssertTransaction HasConsumerId(string consumerId)
    {
        Assert.Equal(consumerId, _transaction.ConsumerId);
        return this;
    }

    public AssertTransaction HasConsumerName(string consumerName)
    {
        Assert.Equal(consumerName, _transaction.ConsumerName);
        return this;
    }

    public AssertTransaction HasConsumerIdType(string consumerIdType)
    {
        Assert.Equal(consumerIdType, _transaction.ConsumerIdType);
        return this;
    }

    public AssertTransaction MeteringPointMasterDataWasSent()
    {
        Assert.Equal(MoveInTransaction.MasterDataState.Sent.ToString(), _transaction.MeteringPointMasterDataState);
        return this;
    }

    public AssertTransaction CustomerMasterDataWasSent()
    {
        Assert.Equal(MoveInTransaction.MasterDataState.Sent.ToString(), _transaction.CustomerMasterDataState);
        return this;
    }

    public AssertTransaction BusinessProcessCompleted()
    {
        Assert.Equal(MoveInTransaction.BusinessProcessState.Completed.ToString(), _transaction.BusinessProcessState);
        return this;
    }

    public AssertTransaction HasEndOfSupplyNotificationState(MoveInTransaction.NotificationState expectedState)
    {
        Assert.Equal(expectedState.ToString(), _transaction.CurrentEnergySupplierNotificationState);
        return this;
    }

    public AssertTransaction HasGridOperatorNotificationState(MoveInTransaction.NotificationState expectedState)
    {
        Assert.Equal(expectedState.ToString(), _transaction.GridOperatorNotificationState);
        return this;
    }

    public AssertTransaction HasCustomerMasterDataSentToGridOperatorState(MoveInTransaction.MasterDataState expectedState)
    {
        Assert.Equal(expectedState.ToString(), _transaction.GridOperator_MessageDeliveryState_CustomerMasterData);
        return this;
    }

    public AssertTransaction HasCustomerMasterData(CustomerMasterData expected)
    {
        var stored = _serializer?.Deserialize(_transaction.CustomerMasterData.ToString(), typeof(CustomerMasterData)) as CustomerMasterData;
        Assert.Equal(expected, stored);
        return this;
    }

    private static dynamic? GetTransaction(string transactionId, IDbConnectionFactory connectionFactory)
    {
        return connectionFactory.GetOpenConnection().QuerySingle(
            $"SELECT * FROM b2b.MoveInTransactions WHERE TransactionId = @TransactionId",
            new
            {
                TransactionId = transactionId,
            });
    }
}
