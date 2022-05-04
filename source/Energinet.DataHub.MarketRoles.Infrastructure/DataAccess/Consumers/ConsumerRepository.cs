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
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.Consumers
{
    public class ConsumerRepository : IConsumerRepository
    {
        private readonly MarketRolesContext _context;

        public ConsumerRepository(MarketRolesContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Add(Consumer consumer)
        {
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));
            _context.Consumers.Add(consumer);
        }

        public Task<Consumer?> GetBySSNAsync(CprNumber cprNumber)
        {
            if (cprNumber == null) throw new ArgumentNullException(nameof(cprNumber));
            return _context.Consumers.SingleOrDefaultAsync(x => x.CprNumber!.Equals(cprNumber));
        }

        public Task<Consumer?> GetByVATNumberAsync(CvrNumber vatNumber)
        {
            if (vatNumber == null) throw new ArgumentNullException(nameof(vatNumber));
            return _context.Consumers.SingleOrDefaultAsync(consumer => consumer.CvrNumber!.Equals(vatNumber));
        }
    }
}
