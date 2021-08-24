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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI;
using Energinet.DataHub.MarketRoles.Infrastructure.PostOffice;
using MediatR;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Outbox.ActorMessages
{
    public class PostOfficeDispatcher : IRequestHandler<PostOfficeEnvelope>
    {
        private readonly IPostOfficeStorageClient _postOfficeStorageClient;

        public PostOfficeDispatcher(
            IPostOfficeStorageClient postOfficeStorageClient)
        {
            _postOfficeStorageClient = postOfficeStorageClient;
        }

        public async Task<Unit> Handle(PostOfficeEnvelope request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            await _postOfficeStorageClient.WriteAsync(request).ConfigureAwait(false);

            return Unit.Value;
        }
    }
}
