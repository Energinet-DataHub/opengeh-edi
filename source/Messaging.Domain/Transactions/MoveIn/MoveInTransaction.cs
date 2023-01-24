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
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Domain.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Messaging.Domain.OutgoingMessages.RejectRequestChangeOfSupplier;
using Messaging.Domain.SeedWork;
using Messaging.Domain.Transactions.MoveIn.Events;
using NodaTime;

namespace Messaging.Domain.Transactions.MoveIn
{
    public class MoveInTransaction : Entity
    {
        private readonly List<OutgoingMessage> _messages = new();
        private readonly ActorNumber _requestedBy;
        private readonly State _state = State.Started;
        private BusinessProcessState _businessProcessState;
        private NotificationState _currentEnergySupplierNotificationState;
        private MasterDataState _meteringPointMasterDataState;
        private NotificationState _gridOperatorNotificationState = NotificationState.Pending;
        private MasterDataState _customerMasterDataForGridOperatorDeliveryState = MasterDataState.Pending;
        private CustomerMasterData? _customerMasterData;

        public MoveInTransaction(TransactionId transactionId, ActorProvidedId actorProvidedId, string marketEvaluationPointId, Instant effectiveDate, string? currentEnergySupplierId, string startedByMessageId, string newEnergySupplierId, string? consumerId, string? consumerName, string? consumerIdType, ActorNumber requestedBy)
        {
            _requestedBy = requestedBy;
            _businessProcessState = BusinessProcessState.Pending;
            _currentEnergySupplierNotificationState = currentEnergySupplierId is not null
                ? NotificationState.Required
                : NotificationState.NotNeeded;
            TransactionId = transactionId;
            ActorProvidedId = actorProvidedId;
            MarketEvaluationPointId = marketEvaluationPointId;
            EffectiveDate = effectiveDate;
            CurrentEnergySupplierId = currentEnergySupplierId;
            StartedByMessageId = startedByMessageId;
            NewEnergySupplierId = newEnergySupplierId;
            ConsumerId = consumerId;
            ConsumerName = consumerName;
            ConsumerIdType = consumerIdType;
            AddDomainEvent(new MoveInWasStarted(TransactionId.Id, _currentEnergySupplierNotificationState));
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

        public TransactionId TransactionId { get; }

        public ActorProvidedId ActorProvidedId { get; }

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
            AddDomainEvent(new BusinessProcessWasCompleted(TransactionId.Id));

            SetCurrentEnergySupplierNotificationToPending();
        }

        public void Accept(string processId)
        {
            if (_state != State.Started)
            {
                throw new MoveInException($"Cannot accept transaction while in state '{_state.ToString()}'");
            }

            if (_businessProcessState == BusinessProcessState.Accepted)
                return;

            _messages.Add(ConfirmRequestChangeOfSupplierMessage.Create(TransactionId, ActorProvidedId, ProcessType.MoveIn, MarketEvaluationPointId, _requestedBy));

            _businessProcessState = BusinessProcessState.Accepted;
            ProcessId = processId ?? throw new ArgumentNullException(nameof(processId));
            AddDomainEvent(new MoveInWasAccepted(ProcessId, MarketEvaluationPointId, TransactionId.Id));
        }

        public void Reject(IReadOnlyList<Reason> reasons)
        {
            if (_businessProcessState == BusinessProcessState.Rejected)
                throw new MoveInException($"Transaction has already been rejected");

            _messages.Add(RejectRequestChangeOfSupplierMessage.Create(
                TransactionId,
                ActorProvidedId,
                ProcessType.MoveIn,
                MarketEvaluationPointId,
                _requestedBy,
                reasons));

            _businessProcessState = BusinessProcessState.Rejected;
            AddDomainEvent(new MoveInWasRejected(TransactionId.Id));
        }

        public void MarkMeteringPointMasterDataAsSent()
        {
            if (_meteringPointMasterDataState != MasterDataState.Pending)
                return;

            _meteringPointMasterDataState = MasterDataState.Sent;
            AddDomainEvent(new MeteringPointMasterDataWasSent(TransactionId.Id));
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

        public void SetCurrentKnownCustomerMasterData(CustomerMasterData customerMasterData)
        {
            _customerMasterData = customerMasterData;
            SendCustomerMasterDataToNewEnergySupplier();
        }

        public void UpdateCustomerMasterData(CustomerMasterData customerMasterData)
        {
            AddDomainEvent(new CustomerMasterDataWasUpdated(TransactionId.Id));
        }

        public void SendCustomerMasterDataToGridOperator(ActorNumber gridOperatorNumber)
        {
            if (_customerMasterDataForGridOperatorDeliveryState == MasterDataState.Sent)
            {
                throw new MoveInException($"Customer master data has already been sent to the grid operator");
            }

            CreateCustomerMasterDataMessage(gridOperatorNumber, MarketRole.GridOperator);
            _customerMasterDataForGridOperatorDeliveryState = MasterDataState.Sent;
        }

        private void SendCustomerMasterDataToNewEnergySupplier()
        {
            ThrowIfMessageExists<CharacteristicsOfACustomerAtAnApMessage>(_requestedBy);
            CreateCustomerMasterDataMessage(_requestedBy, MarketRole.EnergySupplier);
            AddDomainEvent(new CustomerMasterDataWasSent(TransactionId.Id));
        }

        private void ThrowIfMessageExists<TMessage>(ActorNumber receiverId)
        {
            if (_messages.Any(message =>
                    message is TMessage && message.ReceiverId.Equals(receiverId)))
            {
                throw new MoveInException($"Message has already been created and stored");
            }
        }

        private void SetCurrentEnergySupplierNotificationToPending()
        {
            if (_currentEnergySupplierNotificationState == NotificationState.Required && CurrentEnergySupplierId is not null)
            {
                _currentEnergySupplierNotificationState = NotificationState.Pending;
                AddDomainEvent(new EndOfSupplyNotificationChangedToPending(TransactionId.Id, EffectiveDate, MarketEvaluationPointId, CurrentEnergySupplierId));
            }
        }

        private void CreateCustomerMasterDataMessage(ActorNumber receiverNumber, MarketRole receiverRole)
        {
            _messages.Add(CharacteristicsOfACustomerAtAnApMessage.Create(
                TransactionId,
                ActorProvidedId,
                ProcessType.MoveIn,
                receiverNumber,
                receiverRole,
                EffectiveDate,
                _customerMasterData!));
        }
    }
}
