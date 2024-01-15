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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure;

public class OutgoingMessageFileStorage : IOutgoingMessageFileStorage
{
    private readonly IFileStorageClient _fileStorageClient;

    public OutgoingMessageFileStorage(IFileStorageClient fileStorageClient)
    {
        _fileStorageClient = fileStorageClient;
    }

    public async Task<FileStorageReference> UploadAsync(Stream messageStream, ActorNumber receiverActorNumber, Guid id, Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(messageStream);
        ArgumentNullException.ThrowIfNull(receiverActorNumber);
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(timestamp);

        var documentReference = $"{receiverActorNumber.Value}/{timestamp.Year():0000}/{timestamp.Month():00}/{timestamp.Day():00}/{id:N}";

        var reference = FileStorageReference.Create(documentReference);

        await _fileStorageClient.UploadAsync("outgoing", reference.Value, messageStream).ConfigureAwait(false);

        return reference;
    }

    public Task<Stream> DownloadDocumentAsync(FileStorageReference reference)
    {
        throw new NotImplementedException();
    }
}
