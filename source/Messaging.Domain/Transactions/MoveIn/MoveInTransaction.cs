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

using Messaging.Domain.SeedWork;
using Messaging.Domain.Transactions.MoveIn.Events;
using NodaTime;

namespace Messaging.Domain.Transactions.MoveIn
{
    public class MoveInTransaction : Entity
    {
        #pragma warning disable CS0414 // The field will be used later on
        private readonly State _state = State.Started;
        private BusinessProcessState _businessProcessState;
        private EndOfSupplyNotificationState _endOfSupplyNotificationState;
        private MasterDataState _meteringPointMasterDataState;
        private MasterDataState _customerMasterDataState;

        public MoveInTransaction(string transactionId, string marketEvaluationPointId, Instant effectiveDate, string? currentEnergySupplierId, string startedByMessageId, string newEnergySupplierId, string? consumerId, string? consumerName, string? consumerIdType)
        {
            _businessProcessState = BusinessProcessState.Pending;
            _endOfSupplyNotificationState = currentEnergySupplierId is not null
                ? EndOfSupplyNotificationState.Required
                : EndOfSupplyNotificationState.NotNeeded;
            TransactionId = transactionId;
            MarketEvaluationPointId = marketEvaluationPointId;
            EffectiveDate = effectiveDate;
            CurrentEnergySupplierId = currentEnergySupplierId;
            StartedByMessageId = startedByMessageId;
            NewEnergySupplierId = newEnergySupplierId;
            ConsumerId = consumerId;
            ConsumerName = consumerName;
            ConsumerIdType = consumerIdType;
            AddDomainEvent(new MoveInWasStarted(TransactionId, _endOfSupplyNotificationState));
        }

        public enum State
        {
            Started,
            Completed,
        }

        public enum EndOfSupplyNotificationState
        {
            Required,
            NotNeeded,
            Pending,
            EnergySupplierWasNotified,
        }

        public enum BusinessProcessState
        {
            Pending,
            Accepted,
            Rejected,
            Completed,
        }

        public enum MasterDataState
        {
            Pending,
            Sent,
        }

        public string TransactionId { get; }

        public string? ProcessId { get; private set; }

        public string MarketEvaluationPointId { get; }

        public Instant EffectiveDate { get; }

        public string? CurrentEnergySupplierId { get; }

        public string StartedByMessageId { get; }

        public string NewEnergySupplierId { get; }

        public string? ConsumerId { get; }

        public string? ConsumerName { get; }

        public string? ConsumerIdType { get; }

        public void BusinessProcessCompleted()
        {
            if (_businessProcessState != BusinessProcessState.Accepted)
            {
                throw new MoveInException(
                    "Business process can not be set to completed, when it has not been accepted.");
            }

            _businessProcessState = BusinessProcessState.Completed;
            AddDomainEvent(new BusinessProcessWasCompleted(TransactionId));

            SetEndOfSupplyNotificationPending();
        }

        public void AcceptedByBusinessProcess(string processId, string marketEvaluationPointNumber)
        {
            if (_state != State.Started)
            {
                throw new MoveInException($"Cannot accept transaction while in state '{_state.ToString()}'");
            }

            if (_businessProcessState == BusinessProcessState.Accepted)
                return;

            _businessProcessState = BusinessProcessState.Accepted;
            ProcessId = processId ?? throw new ArgumentNullException(nameof(processId));
            AddDomainEvent(new MoveInWasAccepted(ProcessId, marketEvaluationPointNumber, TransactionId));
        }

        public void RejectedByBusinessProcess()
        {
            if (_businessProcessState == BusinessProcessState.Pending)
            {
                _businessProcessState = BusinessProcessState.Rejected;
                AddDomainEvent(new MoveInWasRejected(TransactionId));
            }
        }

        public void MarkMeteringPointMasterDataAsSent()
        {
            if (_meteringPointMasterDataState != MasterDataState.Pending)
                return;

            _meteringPointMasterDataState = MasterDataState.Sent;
            AddDomainEvent(new MeteringPointMasterDataWasSent(TransactionId));
        }

        public void MarkCustomerMasterDataAsSent()
        {
            if (_customerMasterDataState != MasterDataState.Pending)
                return;

            _customerMasterDataState = MasterDataState.Sent;
            AddDomainEvent(new CustomerMasterDataWasSent(TransactionId));
        }

        public void MarkEndOfSupplyNotificationAsSent()
        {
            if (_endOfSupplyNotificationState == EndOfSupplyNotificationState.Pending)
            {
                _endOfSupplyNotificationState = EndOfSupplyNotificationState.EnergySupplierWasNotified;
            }
        }

        private void SetEndOfSupplyNotificationPending()
        {
            if (CurrentEnergySupplierId is null)
                throw new MoveInException("There is no current energy supplier to notify");

            if (_endOfSupplyNotificationState == EndOfSupplyNotificationState.Required)
            {
                _endOfSupplyNotificationState = EndOfSupplyNotificationState.Pending;
                AddDomainEvent(new EndOfSupplyNotificationChangedToPending(TransactionId, EffectiveDate, MarketEvaluationPointId, CurrentEnergySupplierId));
            }
        }
    }
}
