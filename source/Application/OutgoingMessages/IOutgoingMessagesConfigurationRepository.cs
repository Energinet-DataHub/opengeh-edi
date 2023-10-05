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
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Documents;

namespace Energinet.DataHub.EDI.Application.OutgoingMessages
{
    /// <summary>
    /// Store for configuration of outgoing messages
    /// </summary>
    public interface IOutgoingMessagesConfigurationRepository
    {
        /// <summary>
        /// Gets the documentformat to be used with the search parameters. If no elements exists cimXML is returned
        /// </summary>
        /// <param name="actorNumber"></param>
        /// <param name="marketRole"></param>
        /// <param name="documentType"></param>
        /// <returns>Returns the documentformat of the specified actor, role and documenttype</returns>
        Task<DocumentFormat> GetDocumentFormatAsync(ActorNumber actorNumber, MarketRole marketRole, DocumentType documentType);
    }
}
