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

namespace Energinet.DataHub.MarketRoles.Domain.MeteringPoints
{
    /// <summary>
    /// Repository for market evaluation points
    /// </summary>
    public interface IAccountingPointRepository
    {
        /// <summary>
        /// Fetches accounting point by GSRN number
        /// </summary>
        /// <param name="gsrnNumber"></param>
        /// <returns><see cref="AccountingPoint"/></returns>
        Task<AccountingPoint> GetByGsrnNumberAsync(GsrnNumber gsrnNumber);

        /// <summary>
        /// Adds accounting point to repository
        /// </summary>
        /// <param name="accountingPoint"></param>
        void Add(AccountingPoint accountingPoint);

        /// <summary>
        /// Fetches an accounting point by accounting point id
        /// </summary>
        /// <param name="accountingPointId"></param>
        /// <returns><see cref="AccountingPoint"/></returns>
        Task<AccountingPoint> GetByIdAsync(AccountingPointId accountingPointId);
    }
}
