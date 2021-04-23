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
    /// <summary>
    /// Interface for Process Manager
    /// </summary>
    public interface IProcessManager
    {
        /// <summary>
        /// Id for a Process Manager
        /// </summary>
        ProcessId? ProcessId { get; }

        /// <summary>
        /// Effective Date for a Process Manager
        /// </summary>
        Instant? EffectiveDate { get; }

        /// <summary>
        /// Current State for a Process Manager
        /// </summary>
        int State { get; }
    }

    public class ChangeOfSupplierProcessManager : IProcessManager
    {
        private StateEnum _state;

        public ChangeOfSupplierProcessManager()
        {
            SetInternalState(StateEnum.NotStarted);
        }

        public ChangeOfSupplierProcessManager(string? processId, int state, Instant? effectiveDate)
        {
            _state = (StateEnum)state;
            ProcessId = new ProcessId(processId ?? string.Empty);
            EffectiveDate = effectiveDate;
        }

        private enum StateEnum
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

        public ProcessId? ProcessId { get; private set; }

        public Instant? EffectiveDate { get; private set; }

        public int State => (int)_state;

        public List<EnqueuedCommand> CommandsToSend { get; } = new List<EnqueuedCommand>();

        public void When(EnergySupplierChangeRegistered @event)
        {
            switch (_state)
            {
                case StateEnum.NotStarted:
                    ProcessId = @event.ProcessId;
                    EffectiveDate = @event.EffectiveDate;
                    SetInternalState(StateEnum.AwaitingConfirmationMessageDispatch);
                    SendCommand(new SendConfirmationMessage(ProcessId));
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
                case StateEnum.AwaitingConfirmationMessageDispatch:
                    SetInternalState(StateEnum.AwaitingMeteringPointDetailsDispatch);
                    SendCommand(new SendMeteringPointDetails(ProcessId !));
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
                case StateEnum.AwaitingMeteringPointDetailsDispatch:
                    SetInternalState(StateEnum.AwaitingConsumerDetailsDispatch);
                    SendCommand(new SendConsumerDetails(ProcessId!));
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
                case StateEnum.AwaitingConsumerDetailsDispatch:
                    SetInternalState(StateEnum.AwaitingGridOperatorNotification);
                    SendCommand(new NotifyGridOperator(ProcessId!));
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
                case StateEnum.AwaitingGridOperatorNotification:
                    SetInternalState(StateEnum.AwaitingCurrentSupplierNotificationDispatch);
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
                case StateEnum.AwaitingCurrentSupplierNotificationDispatch:
                    SetInternalState(StateEnum.AwaitingSupplierChange);
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
                case StateEnum.AwaitingSupplierChange:
                    SetInternalState(StateEnum.Completed);
                    break;
                default:
                    ThrowIfStateDoesNotMatch(@event);
                    break;
            }
        }

        public bool IsCompleted()
        {
            return _state == StateEnum.Completed;
        }

        private void ScheduleSupplierChange()
        {
            SendCommand(new ChangeSupplier(ProcessId !), EffectiveDate);
        }

        private void ScheduleNotificationOfCurrentSupplier()
        {
            var executionDate = EffectiveDate!.Value.Minus(Duration.FromHours(72));
            SendCommand(new NotifyCurrentSupplier(ProcessId !), executionDate);
        }

        private void ThrowIfStateDoesNotMatch(IDomainEvent @event)
        {
            throw new InvalidProcessManagerStateException($"The event of {@event.GetType().Name} is not applicable when state is {_state.ToString()}.");
        }

        private void SetInternalState(StateEnum state)
        {
            _state = state;
        }

        private void SendCommand(InternalCommand internalCommand, Instant? executionDate = null)
        {
            CommandsToSend.Add(new EnqueuedCommand(internalCommand, executionDate));
        }
    }
}
