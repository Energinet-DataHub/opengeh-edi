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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.ArchivedMessages.Infrastructure;
using MediatR;

namespace Energinet.DataHub.EDI.ArchivedMessages.Application;

public class GetArchivedMessageDocumentQueryHandler : IRequestHandler<GetArchivedMessageDocumentQuery, Stream?>
{
    private readonly IArchivedMessageRepository _archivedMessageRepository;

    public GetArchivedMessageDocumentQueryHandler(IArchivedMessageRepository archivedMessageRepository)
    {
        _archivedMessageRepository = archivedMessageRepository;
    }

    public async Task<Stream?> Handle(GetArchivedMessageDocumentQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _archivedMessageRepository.GetAsync(request.Id, cancellationToken).ConfigureAwait(false);
    }
}
