﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.ArchivedMessages.Infrastructure;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;

namespace Energinet.DataHub.EDI.ArchivedMessages.Application;

public class ArchivedMessagesClient : IArchivedMessagesClient
{
    private readonly IArchivedMessageRepository _archivedMessageRepository;

    public ArchivedMessagesClient(IArchivedMessageRepository archivedMessageRepository)
    {
        _archivedMessageRepository = archivedMessageRepository;
    }

    public async Task CreateAsync(ArchivedMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        await _archivedMessageRepository.AddAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream?> GetAsync(ArchivedMessageId id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);
        return await _archivedMessageRepository.GetAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public Task<MessageSearchResult> SearchAsync(GetMessagesQuery queryInput, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryInput);
        return _archivedMessageRepository.SearchAsync(queryInput, cancellationToken);
    }
}
