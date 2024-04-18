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

using System.Threading;
using System.Threading.Tasks;

namespace Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;

/// <summary>
/// Storage for Process
/// </summary>
public interface IWholesaleServicesProcessRepository
{
    /// <summary>
    /// Adds a new process to database
    /// </summary>
    /// <param name="process"></param>
    void Add(WholesaleServicesProcess process);

    /// <summary>
    /// Gets the process with ID = <paramref name="processId"/>
    /// </summary>
    /// <param name="processId"></param>
    /// <param name="cancellationToken"></param>
    Task<WholesaleServicesProcess> GetAsync(ProcessId processId, CancellationToken cancellationToken);
}
