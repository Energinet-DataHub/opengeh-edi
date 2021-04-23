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

using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses;

namespace Energinet.DataHub.MarketData.Application
{
    /// <summary>
    /// Repository of energy suppliers
    /// </summary>
    public interface IProcessManagerRepository
    {
        /// <summary>
        /// Get an existing Process Manager
        /// </summary>
        /// <param name="processManagerId"></param>
        /// <returns><see cref="ChangeOfSupplierProcessManager"/></returns>
        Task<IProcessManager> GetAsync(ProcessId processManagerId);

        /// <summary>
        /// Add a new Process Manager
        /// </summary>
        /// <param name="processManager"></param>
        void Add(IProcessManager processManager);

        /// <summary>
        /// Save an existing Process Manager
        /// </summary>
        /// <param name="processManager"></param>
        Task SaveAsync(IProcessManager processManager);
    }
}
