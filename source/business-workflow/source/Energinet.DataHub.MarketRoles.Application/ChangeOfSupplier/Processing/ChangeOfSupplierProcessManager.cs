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
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.ConsumerDetails;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.MeteringPointDetails;
using Energinet.DataHub.MarketRoles.Application.Common.Commands;
using Energinet.DataHub.MarketRoles.Application.Common.Processing;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing
{
    public class ChangeOfSupplierProcessManager : ProcessManager
    {
        private State _state;

        public ChangeOfSupplierProcessManager()
            : base()
        {
            SetInternalState(State.NotStarted);
        }

        public enum State
        {
            NotStarted,
            AwaitingConfirmationMessageDispatch,
            AwaitingMeteringPointDetailsDispatch,
            AwaitingConsumerDetailsDispatch,
            AwaitingCurrentSupplierNotificationDispatch,
            AwaitingSupplierChange,
            Completed,
        }

        public void When(EnergySupplierChangeRegistered @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            switch (_state)
            {
                case State.NotStarted:
                    BusinessProcessId = @event.BusinessProcessId;
                    EffectiveDate = @event.EffectiveDate;
                    SetInternalState(State.AwaitingMeteringPointDetailsDispatch);
                    SendCommand(new ForwardMeteringPointDetails(@event.AccountingPointId.Value, @event.BusinessProcessId.Value, @event.Transaction.Value));
                    break;
                default:
                    ThrowIfStateDoesNotMatch(@event);
                    break;
            }
        }

        public void When(MeteringPointDetailsDispatched @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            switch (_state)
            {
                case State.AwaitingMeteringPointDetailsDispatch:
                    SetInternalState(State.AwaitingConsumerDetailsDispatch);
                    SendCommand(new ForwardConsumerDetails(@event.AccountingPointId.Value, @event.BusinessProcessId.Value, @event.Transaction.Value));
                    break;
                default:
                    ThrowIfStateDoesNotMatch(@event);
                    break;
            }
        }

        public void When(ConsumerDetailsDispatched @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            switch (_state)
            {
                case State.AwaitingConsumerDetailsDispatch:
                    SetInternalState(State.AwaitingCurrentSupplierNotificationDispatch);
                    ScheduleNotificationOfCurrentSupplier(@event.AccountingPointId, @event.Transaction);
                    break;
                default:
                    ThrowIfStateDoesNotMatch(@event);
                    break;
            }
        }

        public void When(CurrentSupplierNotified @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            switch (_state)
            {
                case State.AwaitingCurrentSupplierNotificationDispatch:
                    SetInternalState(State.AwaitingSupplierChange);
                    ScheduleSupplierChange(@event.AccountingPointId, @event.Transaction);
                    break;
                default:
                    ThrowIfStateDoesNotMatch(@event);
                    break;
            }
        }

        public void When(EnergySupplierChanged @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            switch (_state)
            {
                case State.AwaitingSupplierChange:
                    SetInternalState(State.Completed);
                    break;
                default:
                    ThrowIfStateDoesNotMatch(@event);
                    break;
            }
        }

        public override bool IsCompleted()
        {
            return _state == State.Completed;
        }

        private void ScheduleSupplierChange(AccountingPointId accountingPointId, Transaction transaction)
        {
            SendCommand(new ChangeSupplier(accountingPointId.Value, transaction.Value), EffectiveDate);
        }

        private void ScheduleNotificationOfCurrentSupplier(AccountingPointId accountingPointId, Transaction transaction)
        {
            var executionDate = EffectiveDate.Minus(Duration.FromHours(72));
            SendCommand(new NotifyCurrentSupplier(accountingPointId.Value, BusinessProcessId.Value, transaction.Value), executionDate);
        }

        private void ThrowIfStateDoesNotMatch(IDomainEvent @event)
        {
            throw new InvalidProcessManagerStateException($"The event of {@event.GetType().Name} is not applicable when state is {_state.ToString()}.");
        }

        private void SetInternalState(State state)
        {
            _state = state;
        }

        private void SendCommand(InternalCommand internalCommand, Instant? executionDate = null)
        {
            CommandsToSend.Add(new EnqueuedCommand(internalCommand, BusinessProcessId, executionDate));
        }
    }
}
