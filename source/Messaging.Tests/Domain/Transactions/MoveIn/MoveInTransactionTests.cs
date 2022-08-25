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

using System.Linq;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Domain.Transactions.MoveIn.Events;
using Xunit;

namespace Messaging.Tests.Domain.Transactions.MoveIn;

public class MoveInTransactionTests
{
    private readonly MoveInTransaction _transaction;

    public MoveInTransactionTests()
    {
        _transaction = CreateTransaction();
    }

    [Fact]
    public void Transaction_is_started()
    {
        var startedEvent = _transaction.DomainEvents.FirstOrDefault(e => e is MoveInWasStarted) as MoveInWasStarted;
        Assert.NotNull(startedEvent);
        Assert.Equal(SampleData.TransactionId, startedEvent?.TransactionId);
        Assert.Equal(MoveInTransaction.EndOfSupplyNotificationState.Required, startedEvent?.EndOfSupplyNotificationState);
    }

    [Fact]
    public void Business_process_is_set_to_accepted()
    {
        _transaction.AcceptedByBusinessProcess(SampleData.ProcessId, SampleData.MarketEvaluationPointId);

        var acceptedEvent = _transaction.DomainEvents.FirstOrDefault(e => e is MoveInWasAccepted) as MoveInWasAccepted;
        Assert.NotNull(acceptedEvent);
        Assert.Equal(SampleData.ProcessId, acceptedEvent?.BusinessProcessId);
    }

    [Fact]
    public void Business_process_can_be_marked_as_accepted_once_only()
    {
        _transaction.AcceptedByBusinessProcess(SampleData.ProcessId, SampleData.MarketEvaluationPointId);

        _transaction.AcceptedByBusinessProcess(SampleData.ProcessId, SampleData.MarketEvaluationPointId);

        Assert.Equal(1, _transaction.DomainEvents.Count(e => e is MoveInWasAccepted));
    }

    [Fact]
    public void Business_process_can_be_set_to_rejected()
    {
        _transaction.RejectedByBusinessProcess();

        var rejectedEvent = _transaction.DomainEvents.FirstOrDefault(e => e is MoveInWasRejected) as MoveInWasRejected;
        Assert.NotNull(rejectedEvent);
        Assert.Equal(SampleData.TransactionId, rejectedEvent?.TransactionId);
    }

    [Fact]
    public void Business_process_can_be_marked_as_rejected_once_only()
    {
        _transaction.RejectedByBusinessProcess();

        _transaction.RejectedByBusinessProcess();

        Assert.Equal(1, _transaction.DomainEvents.Count(e => e is MoveInWasRejected));
    }

    [Fact]
    public void Business_process_can_not_set_to_completed_when_it_has_not_accepted()
    {
        Assert.Throws<MoveInException>(() => _transaction.BusinessProcessCompleted());
    }

    [Fact]
    public void Business_process_is_completed()
    {
        _transaction.AcceptedByBusinessProcess(SampleData.ProcessId, SampleData.MarketEvaluationPointId);
        _transaction.BusinessProcessCompleted();

        Assert.Contains(_transaction.DomainEvents, e => e is BusinessProcessWasCompleted);
    }

    [Fact]
    public void End_of_supply_notification_status_is_changed_to_pending_when_business_process_is_completed()
    {
        _transaction.AcceptedByBusinessProcess(SampleData.ProcessId, SampleData.MarketEvaluationPointId);
        _transaction.BusinessProcessCompleted();

        Assert.Contains(_transaction.DomainEvents, e => e is EndOfSupplyNotificationChangedToPending);
    }

    [Fact]
    public void End_of_supply_notification_is_not_needed_when_no_current_energy_supplier_is_present()
    {
        var transaction = CreateTransaction(currentEnergySupplierId: null);

        var startedEvent = transaction.DomainEvents.FirstOrDefault(e => e is MoveInWasStarted) as MoveInWasStarted;
        Assert.Equal(MoveInTransaction.EndOfSupplyNotificationState.NotNeeded, startedEvent?.EndOfSupplyNotificationState);
    }

    [Fact]
    public void Customer_master_data_is_sent()
    {
        _transaction.MarkCustomerMasterDataAsSent();

        var domainEvent = _transaction.DomainEvents.FirstOrDefault(e => e is CustomerMasterDataWasSent) as CustomerMasterDataWasSent;
        Assert.NotNull(domainEvent);
        Assert.Equal(_transaction.TransactionId, domainEvent?.TransactionId);
    }

    [Fact]
    public void Metering_point_master_data_is_sent()
    {
        _transaction.MarkMeteringPointMasterDataAsSent();

        var domainEvent = _transaction.DomainEvents.FirstOrDefault(e => e is MeteringPointMasterDataWasSent) as MeteringPointMasterDataWasSent;
        Assert.NotNull(domainEvent);
        Assert.Equal(_transaction.TransactionId, domainEvent?.TransactionId);
    }

    private static MoveInTransaction CreateTransaction()
    {
        return CreateTransaction(SampleData.CurrentEnergySupplierId);
    }

    private static MoveInTransaction CreateTransaction(string? currentEnergySupplierId)
    {
        return new MoveInTransaction(
            SampleData.TransactionId,
            SampleData.MarketEvaluationPointId,
            SampleData.EffectiveDate,
            currentEnergySupplierId,
            SampleData.StartedByMessageId,
            SampleData.NewEnergySupplierId,
            SampleData.ConsumerId,
            SampleData.ConsumerName,
            SampleData.ConsumerIdType);
    }

    private void AssertTransactionIsNotCompleted()
    {
        Assert.DoesNotContain(_transaction.DomainEvents, e => e is MoveInWasCompleted);
    }

    private void AssertTransactionIsCompleted()
    {
        Assert.Contains(_transaction.DomainEvents, e => e is MoveInWasCompleted);
    }
}
