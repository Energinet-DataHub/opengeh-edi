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

using System.Collections.Generic;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process.Commands;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process.Events;
using Energinet.DataHub.MarketData.Application.Common.Commands;
using Energinet.DataHub.MarketData.Application.Common.Process;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process
{
    public class ChangeOfSupplierProcessManager
    {
        private ProcessId? _processId;
        private Instant? _effectiveDate;
        private State _state;

        public ChangeOfSupplierProcessManager()
        {
            SetInternalState(State.NotStarted);
        }

        private enum State
        {
            NotStarted,
            AwaitingConfirmationMessageDispatch,
            AwaitingMeteringPointDetailsDispatch,
            AwaitingConsumerDetailsDispatch,
            AwaitingGridOperatorNotification,
            AwaitingCurrentSupplierNotificationDispatch,
            AwaitingSupplierChange,
            Completed,
        }

        public List<EnqueuedCommand> CommandsToSend { get; } = new List<EnqueuedCommand>();

        public void When(EnergySupplierChangeRegistered @event)
        {
            switch (_state)
            {
                case State.NotStarted:
                    _processId = @event.ProcessId;
                    _effectiveDate = @event.EffectiveDate;
                    SetInternalState(State.AwaitingConfirmationMessageDispatch);
                    SendCommand(new SendConfirmationMessage(_processId));
                    break;
                default:
                    ThrowIfStateDoesNotMatch(@event);
                    break;
            }
        }

        public void When(ConfirmationMessageDispatched @event)
        {
            switch (_state)
            {
                case State.AwaitingConfirmationMessageDispatch:
                    SetInternalState(State.AwaitingMeteringPointDetailsDispatch);
                    SendCommand(new SendMeteringPointDetails(_processId !));
                    break;
                default:
                    ThrowIfStateDoesNotMatch(@event);
                    break;
            }
        }

        public void When(MeteringPointDetailsDispatched @event)
        {
            switch (_state)
            {
                case State.AwaitingMeteringPointDetailsDispatch:
                    SetInternalState(State.AwaitingConsumerDetailsDispatch);
                    SendCommand(new SendConsumerDetails(_processId!));
                    break;
                default:
                    ThrowIfStateDoesNotMatch(@event);
                    break;
            }
        }

        public void When(ConsumerDetailsDispatched @event)
        {
            switch (_state)
            {
                case State.AwaitingConsumerDetailsDispatch:
                    SetInternalState(State.AwaitingGridOperatorNotification);
                    SendCommand(new NotifyGridOperator(_processId!));
                    break;
                default:
                    ThrowIfStateDoesNotMatch(@event);
                    break;
            }
        }

        public void When(GridOperatorNotified @event)
        {
            switch (_state)
            {
                case State.AwaitingGridOperatorNotification:
                    SetInternalState(State.AwaitingCurrentSupplierNotificationDispatch);
                    ScheduleNotificationOfCurrentSupplier();
                    break;
                default:
                    ThrowIfStateDoesNotMatch(@event);
                    break;
            }
        }

        public void When(CurrentSupplierNotified @event)
        {
            switch (_state)
            {
                case State.AwaitingCurrentSupplierNotificationDispatch:
                    SetInternalState(State.AwaitingSupplierChange);
                    ScheduleSupplierChange();
                    break;
                default:
                    ThrowIfStateDoesNotMatch(@event);
                    break;
            }
        }

        public void When(EnergySupplierChanged @event)
        {
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

        public bool IsCompleted()
        {
            return _state == State.Completed;
        }

        private void ScheduleSupplierChange()
        {
            SendCommand(new ChangeSupplier(_processId !), _effectiveDate);
        }

        private void ScheduleNotificationOfCurrentSupplier()
        {
            var executionDate = _effectiveDate!.Value.Minus(Duration.FromHours(72));
            SendCommand(new NotifyCurrentSupplier(_processId !), executionDate);
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
            CommandsToSend.Add(new EnqueuedCommand(internalCommand, executionDate));
        }
    }
}
