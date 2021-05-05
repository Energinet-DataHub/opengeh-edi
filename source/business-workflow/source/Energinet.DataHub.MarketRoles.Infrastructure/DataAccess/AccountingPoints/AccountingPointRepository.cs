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
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.AccountingPoints
{
    public class AccountingPointRepository : IAccountingPointRepository
    {
        private readonly MarketRolesContext _context;

        public AccountingPointRepository(MarketRolesContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<AccountingPoint> GetByGsrnNumberAsync(GsrnNumber gsrnNumber)
        {
            if (gsrnNumber == null) throw new ArgumentNullException(nameof(gsrnNumber));
            return _context.AccountingPoints.SingleOrDefaultAsync(x => x.GsrnNumber.Equals(gsrnNumber));
        }

        public void Add(AccountingPoint accountingPoint)
        {
            if (accountingPoint == null) throw new ArgumentNullException(nameof(accountingPoint));
            _context.AccountingPoints.Add(accountingPoint);
        }
    }
}
