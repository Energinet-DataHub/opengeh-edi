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

using Messaging.Domain.Actors;
using Messaging.Domain.SeedWork;
using Messaging.Domain.Transactions.MoveIn.Events;
using NodaTime;

namespace Messaging.Domain.Transactions.MoveIn
{
    public class MoveInTransaction : Entity
    {
        private readonly ActorNumber _requestedBy;
        private readonly State _state = State.Started;
        private BusinessProcessState _businessProcessState;
        private NotificationState _currentEnergySupplierNotificationState;
        private MasterDataState _meteringPointMasterDataState;
        private MasterDataState _customerMasterDataState;
        private NotificationState _gridOperatorNotificationState = NotificationState.Pending;
        private MasterDataState _customerMasterDataForGridOperatorDeliveryState;
        private CustomerMasterData? _customerMasterData;

        public MoveInTransaction(string transactionId, string marketEvaluationPointId, Instant effectiveDate, string? currentEnergySupplierId, string startedByMessageId, string newEnergySupplierId, string? consumerId, string? consumerName, string? consumerIdType, ActorNumber requestedBy)
        {
            _requestedBy = requestedBy;
            _customerMasterDataForGridOperatorDeliveryState = MasterDataState.Pending;
            _businessProcessState = BusinessProcessState.Pending;
            _currentEnergySupplierNotificationState = currentEnergySupplierId is not null
                ? NotificationState.Required
                : NotificationState.NotNeeded;
            TransactionId = transactionId;
            MarketEvaluationPointId = marketEvaluationPointId;
            EffectiveDate = effectiveDate;
            CurrentEnergySupplierId = currentEnergySupplierId;
            StartedByMessageId = startedByMessageId;
            NewEnergySupplierId = newEnergySupplierId;
            ConsumerId = consumerId;
            ConsumerName = consumerName;
            ConsumerIdType = consumerIdType;
            AddDomainEvent(new MoveInWasStarted(TransactionId, _currentEnergySupplierNotificationState));
        }

        public enum State
        {
            Started,
            Completed,
        }

        public enum NotificationState
        {
            Required,
            NotNeeded,
            Pending,
            WasNotified,
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

        public CustomerMasterData? CustomerMasterData => _customerMasterData;

        public void BusinessProcessCompleted()
        {
            if (_businessProcessState != BusinessProcessState.Accepted)
            {
                throw new MoveInException(
                    "Business process can not be set to completed, when it has not been accepted.");
            }

            _businessProcessState = BusinessProcessState.Completed;
            AddDomainEvent(new BusinessProcessWasCompleted(TransactionId));

            SetCurrentEnergySupplierNotificationToPending();
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

        public void SetCurrentEnergySupplierWasNotified()
        {
            if (_currentEnergySupplierNotificationState == NotificationState.Pending)
            {
                _currentEnergySupplierNotificationState = NotificationState.WasNotified;
            }
        }

        public void SetGridOperatorWasNotified()
        {
            if (_gridOperatorNotificationState == NotificationState.Pending)
            {
                _gridOperatorNotificationState = NotificationState.WasNotified;
                AddDomainEvent(new GridOperatorWasNotified());
            }
        }

        public void SetCustomerMasterDataDeliveredWasToGridOperator()
        {
            if (_customerMasterDataForGridOperatorDeliveryState == MasterDataState.Pending)
                _customerMasterDataForGridOperatorDeliveryState = MasterDataState.Sent;
        }

        public void ReceiveCustomerMasterData(CustomerMasterData customerMasterData)
        {
            if (_customerMasterData is null)
            {
                _customerMasterData = customerMasterData;
                AddDomainEvent(new CustomerMasterDataWasReceived(TransactionId));
            }
        }

        private void SetCurrentEnergySupplierNotificationToPending()
        {
            if (_currentEnergySupplierNotificationState == NotificationState.Required && CurrentEnergySupplierId is not null)
            {
                _currentEnergySupplierNotificationState = NotificationState.Pending;
                AddDomainEvent(new EndOfSupplyNotificationChangedToPending(TransactionId, EffectiveDate, MarketEvaluationPointId, CurrentEnergySupplierId));
            }
        }
    }
}
