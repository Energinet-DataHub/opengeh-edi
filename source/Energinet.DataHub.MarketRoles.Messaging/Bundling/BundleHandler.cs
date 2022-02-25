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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.MessageHub.Bundling;
using Energinet.DataHub.MarketRoles.Infrastructure.LocalMessageHub;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using MediatR;

namespace Energinet.DataHub.MarketRoles.Messaging.Bundling
{
    public abstract class BundleHandler<TDocument> : IRequestHandler<BundleRequest<TDocument>, string>
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IDocumentSerializer<TDocument> _documentSerializer;

        protected BundleHandler(
            IJsonSerializer jsonSerializer,
            IDocumentSerializer<TDocument> documentSerializer)
        {
            _jsonSerializer = jsonSerializer;
            _documentSerializer = documentSerializer;
        }

        public Task<string> Handle(BundleRequest<TDocument> request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var documents = GetDocuments(request.Documents);
            var bundle = _documentSerializer.Serialize(documents);
            return Task.FromResult(bundle);
        }

        private List<TDocument> GetDocuments(IList<MessageHubMessage> messages)
        {
            var documents = messages.Select(message => _jsonSerializer.Deserialize<TDocument>(message.Content));
            return documents.ToList();
        }
    }
}
