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
using System.Linq;
using Messaging.Application.Transactions;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Domain.Transactions.MoveIn.Events;
using Xunit;

namespace Messaging.Tests.Transactions.MoveIn;

public class MoveInTransactionTests
{
    [Fact]
    public void Transaction_is_started()
    {
        var transaction = CreateTransaction();

        var startedEvent = transaction.DomainEvents.FirstOrDefault(e => e is MoveInWasStarted) as MoveInWasStarted;
        Assert.NotNull(startedEvent);
        Assert.Equal(SampleData.TransactionId, startedEvent?.TransactionId);
    }

    [Fact]
    public void Transaction_is_accepted()
    {
        var transaction = CreateTransaction();

        transaction.AcceptedByBusinessProcess(SampleData.ProcessId, SampleData.MarketEvaluationPointId);

        var acceptedEvent = transaction.DomainEvents.FirstOrDefault(e => e is MoveInWasAccepted) as MoveInWasAccepted;
        Assert.NotNull(acceptedEvent);
        Assert.Equal(SampleData.ProcessId, acceptedEvent?.BusinessProcessId);
    }

    [Fact]
    public void Transaction_can_only_be_accepted_while_in_the_state_of_started()
    {
        var transaction = CreateTransaction();

        transaction.AcceptedByBusinessProcess(SampleData.ProcessId, SampleData.MarketEvaluationPointId);

        Assert.Throws<MoveInException>(() => transaction.AcceptedByBusinessProcess(SampleData.ProcessId, SampleData.MarketEvaluationPointId));
    }

    [Fact]
    public void Transaction_is_rejected()
    {
        var transaction = CreateTransaction();

        transaction.RejectedByBusinessProcess();

        var rejectedEvent = transaction.DomainEvents.FirstOrDefault(e => e is MoveInWasRejected) as MoveInWasRejected;
        Assert.NotNull(rejectedEvent);
        Assert.Equal(SampleData.TransactionId, rejectedEvent?.TransactionId);
    }

    [Fact]
    public void Transaction_can_only_be_rejected_while_in_the_state_of_started()
    {
        var transaction = CreateTransaction();

        transaction.RejectedByBusinessProcess();

        Assert.Throws<MoveInException>(() => transaction.RejectedByBusinessProcess());
    }

    [Fact]
    public void Transaction_is_completed_if_business_request_is_rejected()
    {
        var transaction = CreateTransaction();

        transaction.RejectedByBusinessProcess();

        Assert.Contains(transaction.DomainEvents, e => e is MoveInWasCompleted);
    }

    [Fact]
    public void Transaction_can_complete_when_started()
    {
        var transaction = CreateTransaction();
        transaction.AcceptedByBusinessProcess(SampleData.ProcessId, SampleData.MarketEvaluationPointId);
        transaction.HasForwardedMeteringPointMasterData();

        transaction.Complete();

        Assert.Contains(transaction.DomainEvents, e => e is MoveInWasCompleted);
    }

    [Fact]
    public void Can_not_complete_transaction_if_already_completed()
    {
        var transaction = CreateTransaction();
        transaction.AcceptedByBusinessProcess(SampleData.ProcessId, SampleData.MarketEvaluationPointId);
        transaction.HasForwardedMeteringPointMasterData();
        transaction.Complete();

        Assert.Throws<MoveInException>(() => transaction.Complete());
    }

    // [Fact]
    // public void Metering_point_master_data_must_have_been_forwarded_when_accepted()
    // {
    //     var transaction = CreateTransaction();
    //     transaction.AcceptedByBusinessProcess(SampleData.ProcessId);
    //
    //     Assert.Throws<MoveInException>(() => transaction.Complete());
    // }
    private static MoveInTransaction CreateTransaction()
    {
        return new MoveInTransaction(
            SampleData.TransactionId,
            SampleData.MarketEvaluationPointId,
            SampleData.EffectiveDate,
            SampleData.CurrentEnergySupplierId,
            SampleData.StartedByMessageId,
            SampleData.NewEnergySupplierId,
            SampleData.ConsumerId,
            SampleData.ConsumerName,
            SampleData.ConsumerIdType);
    }
}
