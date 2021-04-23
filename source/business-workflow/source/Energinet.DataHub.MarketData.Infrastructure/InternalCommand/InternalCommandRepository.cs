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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Infrastructure.DatabaseAccess.Write;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    public class InternalCommandRepository : IInternalCommandRepository
    {
        private readonly IWriteDatabaseContext _writeDatabaseContext;
        private readonly IClock _clock;

        public InternalCommandRepository(IWriteDatabaseContext writeDatabaseContext, IClock clock)
        {
            _writeDatabaseContext = writeDatabaseContext;
            _clock = clock;
        }

        public async Task<InternalCommand?> GetUnprocessedInternalCommandAsync()
        {
            return await _writeDatabaseContext.InternalCommandDataModels
                .Where(x => !x.ProcessedDate.HasValue)
                .Where(x => !x.ScheduledDate.HasValue || x.ScheduledDate.Value <= _clock.GetCurrentInstant())
                .Select(x => new InternalCommand
                {
                    Id = x.Id,
                    Data = x.Data,
                    Type = x.Type,
                })
                .FirstOrDefaultAsync();
        }
    }
}
