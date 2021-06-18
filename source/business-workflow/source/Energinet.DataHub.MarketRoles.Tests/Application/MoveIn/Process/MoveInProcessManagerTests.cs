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
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Application.MoveIn.Processing;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.MarketRoles.Tests.Application.MoveIn.Process
{
    #pragma warning disable
    public class MoveInProcessManagerTests
    {
        private readonly Guid _accountingPointId = Guid.NewGuid();
        private readonly Guid _businessProcessId = Guid.NewGuid();
        private readonly string _transaction = Guid.NewGuid().ToString();
        private readonly Guid _consumerId = Guid.NewGuid();
        private readonly Guid _energySupplierId = Guid.NewGuid();
        private readonly Instant _moveInDate = SystemClock.Instance.GetCurrentInstant();

        [Fact]
        public void ConsumerMoveInAccepted_WhenStateIsNotStarted_EffectuationIsScheduled()
        {
            var processManager = new MoveInProcessManager();

            processManager.When(new ConsumerMoveInAccepted(_accountingPointId, SampleData.GsrnNumber, _businessProcessId, _transaction, _consumerId, _energySupplierId, _moveInDate));

            var command =
                processManager.CommandsToSend.First(c => c.Command is EffectuateConsumerMoveIn).Command as
                    EffectuateConsumerMoveIn;

            Assert.NotNull(command);
        }

        [Fact]
        public void ConsumerMovedIn_WhenStateIsAwaitingEffectuation_ProcessIsCompleted()
        {
            var processManager = new MoveInProcessManager();

            processManager.When(new ConsumerMoveInAccepted(_accountingPointId, SampleData.GsrnNumber, _businessProcessId, _transaction, _consumerId, _energySupplierId, _moveInDate));
            processManager.When(new ConsumerMovedIn(_accountingPointId, SampleData.GsrnNumber, _businessProcessId, _consumerId, _moveInDate));

            Assert.True(processManager.IsCompleted());
        }
    }
}
