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
using System.Threading;
using System.Threading.Tasks;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Transactions.AggregatedMeasureData;

public class AggregatedMeasureDataProcessRepository : IAggregatedMeasureDataProcessRepository
{
    private readonly B2BContext _b2BContext;

    public AggregatedMeasureDataProcessRepository(B2BContext b2BContext)
    {
        _b2BContext = b2BContext;
    }

    public void Add(AggregatedMeasureDataProcess process)
    {
        _b2BContext.AggregatedMeasureDataProcesses.Add(process);
    }

    public async Task<AggregatedMeasureDataProcess?> GetByIdAsync(ProcessId processId, CancellationToken cancellationToken)
    {
        return await _b2BContext
            .AggregatedMeasureDataProcesses
            .FirstOrDefaultAsync(process => process.ProcessId == processId, cancellationToken)
            .ConfigureAwait(false);
    }
}
