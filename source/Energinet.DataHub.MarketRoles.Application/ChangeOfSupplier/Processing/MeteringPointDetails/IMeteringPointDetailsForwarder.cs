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

namespace Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.MeteringPointDetails
{
    /// <summary>
    /// Handler for forwarding accounting point details to future energy supplier. This handler is used
    /// as part of a change of supplier business process
    /// </summary>
    public interface IMeteringPointDetailsForwarder
    {
        /// <summary>
        /// Generate and dispatch consumer details
        /// </summary>
        /// <param name="accountingPointId"></param>
        /// <returns><see cref="Task"/></returns>
        Task ForwardAsync(AccountingPointId accountingPointId);
    }
}
