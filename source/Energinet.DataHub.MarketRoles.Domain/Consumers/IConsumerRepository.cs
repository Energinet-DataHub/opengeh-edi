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

namespace Energinet.DataHub.MarketRoles.Domain.Consumers
{
    /// <summary>
    /// Repository for consumer entities
    /// </summary>
    public interface IConsumerRepository
    {
        /// <summary>
        /// Adds a new consumer to repository
        /// </summary>
        /// <param name="consumer"></param>
        void Add(Consumer consumer);

        /// <summary>
        /// Find consumer by CPR number
        /// </summary>
        /// <param name="cprNumber"></param>
        /// <returns><see cref="Consumer"/></returns>
        Task<Consumer?> GetBySSNAsync(CprNumber cprNumber);

        /// <summary>
        /// Find consumer by VAT-number
        /// </summary>
        /// <param name="vatNumber"></param>
        /// <returns><see cref="Consumer"/></returns>
        Task<Consumer?> GetByVATNumberAsync(CvrNumber vatNumber);
    }
}
