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

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI
{
    /// <summary>
    /// Dispatches messages to message hub
    /// </summary>
    public interface IMessageHubDispatcher
    {
        /// <summary>
        /// Dispatch message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="documentType">Type of document</param>
        /// <param name="recipient">Identifier of the recipient of the message.</param>
        /// <param name="gsrnNumber">Gsrn number of a metering point</param>
        /// <typeparam name="TMessage">Message to be delivered to message hub</typeparam>
        /// <returns><see cref="Task"/></returns>
        Task DispatchAsync<TMessage>(TMessage message, DocumentType documentType, string recipient, string gsrnNumber);
    }
}
