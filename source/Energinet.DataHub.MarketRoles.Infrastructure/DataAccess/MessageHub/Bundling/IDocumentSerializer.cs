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

using System.Collections.Generic;

namespace Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.MessageHub.Bundling
{
    /// <summary>
    /// Serialize documents.
    /// </summary>
    /// <typeparam name="TDocument">Document type to serialize</typeparam>
    public interface IDocumentSerializer<TDocument>
    {
        /// <summary>
        /// Serialize single document.
        /// </summary>
        /// <param name="message"></param>
        public string Serialize(TDocument message);

        /// <summary>
        /// Serialize multiple documents into one.
        /// </summary>
        /// <param name="messages"></param>
        public string Serialize(IList<TDocument> messages);
    }
}
