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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.ArchivedMessages.Interfaces;

/// <summary>
/// Client for interacting with the archived messages service
/// </summary>
public interface IArchivedMessagesClient
{
    /// <summary>
    /// Create a new archived message directly in the database
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    Task<IArchivedFile> CreateAsync(ArchivedMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Get a document from the database
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    Task<ArchivedMessageStream?> GetAsync(ArchivedMessageId id, CancellationToken cancellationToken);

    /// <summary>
    /// Search for messages in the database
    /// </summary>
    /// <param name="queryInput"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    Task<MessageSearchResult> SearchAsync(GetMessagesQuery queryInput, CancellationToken cancellationToken);
}
