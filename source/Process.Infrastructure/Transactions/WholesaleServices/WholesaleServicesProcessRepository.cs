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

using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Exceptions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Transactions.WholesaleServices;

public class WholesaleServicesProcessRepository : IWholesaleServicesProcessRepository
{
    private readonly ProcessContext _processContext;

    public WholesaleServicesProcessRepository(ProcessContext processContext)
    {
        _processContext = processContext;
    }

    public void Add(WholesaleServicesProcess process)
    {
        _processContext.WholesaleServicesProcesses.Add(process);
    }

    public async Task<WholesaleServicesProcess> GetAsync(ProcessId processId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(processId);
        return await _processContext
                   .WholesaleServicesProcesses
                   .FirstOrDefaultAsync(process => process.ProcessId == processId, cancellationToken)
                   .ConfigureAwait(false)
               ?? throw ProcessNotFoundException.ProcessForProcessIdNotFound(processId.Id);
    }
}
