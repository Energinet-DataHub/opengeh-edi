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

public class OutgoingMessageDocumentClient : IOutgoingMessageDocumentClient
{
    private readonly IFileStorageClient _fileStorageClient;

    public OutgoingMessageDocumentClient(IFileStorageClient fileStorageClient)
    {
        _fileStorageClient = fileStorageClient;
    }

    public async Task<UploadedDocumentReference> UploadDocumentAsync(Stream marketDocumentFile, ActorNumber receiverActorNumber, BundleId bundleId, Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(marketDocumentFile);
        ArgumentNullException.ThrowIfNull(receiverActorNumber);
        ArgumentNullException.ThrowIfNull(bundleId);
        ArgumentNullException.ThrowIfNull(timestamp);

        var documentReference = $"outgoing/{receiverActorNumber.Value}/{timestamp.Year()}/{timestamp.Month()}/{timestamp.Day()}/{bundleId.Id:N}";

        var reference = UploadedDocumentReference.Create(documentReference);

        var referenceWithFolder = $"outgoing/{reference.Value}";

        referenceWithFolder = $"{bundleId.Id:N}";
        await _fileStorageClient.UploadAsync(referenceWithFolder, marketDocumentFile).ConfigureAwait(false);

        return reference;
    }

    public Task<Stream> DownloadDocumentAsync(UploadedDocumentReference reference)
    {
        throw new NotImplementedException();
    }
}
