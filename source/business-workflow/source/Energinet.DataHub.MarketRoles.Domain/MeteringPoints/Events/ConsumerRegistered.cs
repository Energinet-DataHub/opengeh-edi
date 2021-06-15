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
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events
{
    public class ConsumerRegistered : DomainEventBase
    {
        public ConsumerRegistered(Guid accountingPointId, string gsrnNumber, Guid businessProcessId, string transaction, Guid consumerId, Instant moveInDate)
        {
            AccountingPointId = accountingPointId;
            GsrnNumber = gsrnNumber;
            BusinessProcessId = businessProcessId;
            Transaction = transaction;
            ConsumerId = consumerId;
            MoveInDate = moveInDate;
        }

        public Guid AccountingPointId { get; }

        public string GsrnNumber { get; }

        public Guid BusinessProcessId { get; }

        public string Transaction { get; }

        public Guid ConsumerId { get; }

        public Instant MoveInDate { get; }
    }
}
