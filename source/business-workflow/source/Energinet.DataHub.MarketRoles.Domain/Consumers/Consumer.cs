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

namespace Energinet.DataHub.MarketRoles.Domain.Consumers
{
    public class Consumer : AggregateRootBase
    {
        public Consumer(ConsumerId consumerId, CprNumber cprNumber)
        {
            ConsumerId = consumerId ?? throw new ArgumentNullException(nameof(consumerId));
            CprNumber = cprNumber ?? throw new ArgumentNullException(nameof(cprNumber));
        }

        public Consumer(ConsumerId consumerId, CvrNumber cvrNumber)
        {
            ConsumerId = consumerId ?? throw new ArgumentNullException(nameof(consumerId));
            CvrNumber = cvrNumber ?? throw new ArgumentNullException(nameof(cvrNumber));
        }

        private Consumer()
        {
            // EF Core only
        }

        public ConsumerId ConsumerId { get; } = null!;

        public CprNumber? CprNumber { get; }

        public CvrNumber? CvrNumber { get; }
    }
}
