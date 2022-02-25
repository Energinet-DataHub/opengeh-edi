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
using Energinet.DataHub.MarketRoles.Infrastructure.EDI;

namespace Energinet.DataHub.MarketRoles.Messaging
{
    /// <summary>
    /// Local Post Office
    /// </summary>
    public interface ILocalMessageHubDataAvailableClient
    {
        /// <summary>
        /// Dispatch message to Local Post Office
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        void DataAvailable(MessageHubEnvelope messageHubEnvelope);
    }
}
