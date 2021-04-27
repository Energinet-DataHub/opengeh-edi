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

using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints
{
    internal class ConsumerRegistration : Entity
    {
        public ConsumerRegistration(ConsumerId consumerId, ProcessId processId)
        {
            ConsumerId = consumerId;
            ProcessId = processId;
        }

        private ConsumerRegistration(ConsumerId consumerId, Instant moveInDate, ProcessId processId)
        {
            ConsumerId = consumerId;
            MoveInDate = moveInDate;
            ProcessId = processId;
        }

        public ConsumerId ConsumerId { get; }

        public Instant MoveInDate { get; }

        public ProcessId ProcessId { get; }

        public static ConsumerRegistration CreateFrom(ConsumerRegistrationSnapshot snapshot)
        {
            return new ConsumerRegistration(new ConsumerId(snapshot.ConsumerId), snapshot.MoveInDate, new ProcessId(snapshot.ProcessId));
        }

        public ConsumerRegistrationSnapshot GetSnapshot()
        {
            return new ConsumerRegistrationSnapshot(ConsumerId.Value, MoveInDate, ProcessId.Value !);
        }
    }
}
