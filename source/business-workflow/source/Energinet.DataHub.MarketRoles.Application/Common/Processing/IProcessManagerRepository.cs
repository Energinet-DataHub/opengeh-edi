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
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;

namespace Energinet.DataHub.MarketRoles.Application.Common.Processing
{
    /// <summary>
    /// Repository of energy suppliers
    /// </summary>
    public interface IProcessManagerRepository
    {
        /// <summary>
        /// Get an existing Process Manager
        /// </summary>
        /// <param name="businessProcessId"></param>
        /// <returns><see cref="ProcessManager"/></returns>
        Task<TProcessManager?> GetAsync<TProcessManager>(BusinessProcessId businessProcessId)
            where TProcessManager : ProcessManager;

        /// <summary>
        /// Add a new Process Manager
        /// </summary>
        /// <param name="processManager"></param>
        void Add<TProcessManager>(TProcessManager processManager)
            where TProcessManager : ProcessManager;
    }
}
