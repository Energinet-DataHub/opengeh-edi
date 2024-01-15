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
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure;

/// <summary>
/// Client for saving outgoing message market documents
/// </summary>
public interface IOutgoingMessageFileStorage
{
    /// <summary>
    /// Upload the the outgoing message market document
    /// </summary>
    /// <param name="messageStream">The market document to upload, typically created by the DocumentFactory</param>
    /// <param name="receiverActorNumber">The Actor Number for the actor receiving the market document</param>
    /// <param name="id">The id for the outgoing message</param>
    /// <param name="timestamp">The timestamp from the created market document</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    Task<FileStorageReference> UploadAsync(Stream messageStream, ActorNumber receiverActorNumber, Guid id, Instant timestamp);

    /// <summary>
    /// Download a market document file by its unique reference
    /// </summary>
    /// <param name="reference">A <see cref="FileStorageReference"/> typically found on a <see cref="MarketDocument"/></param>
    Task<Stream> DownloadDocumentAsync(FileStorageReference reference);
}
