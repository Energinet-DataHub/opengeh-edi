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

using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands
{
    /// <summary>
    /// Access provider for queued internal commands
    /// </summary>
    public interface IInternalCommandAccessor
    {
        /// <summary>
        /// Returns all queued internal commands that have not been marked as dispatched
        /// </summary>
        /// <returns><see cref="QueuedInternalCommand"/></returns>
        Task<ImmutableList<QueuedInternalCommand>> GetUndispatchedAsync();
    }
}
