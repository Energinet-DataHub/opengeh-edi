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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;

#pragma warning disable CA1711 // Is a "Stream" value object
public record MarketDocumentStream : IMarketDocumentStream
{
    private readonly Stream _stream;

    public MarketDocumentStream(IArchivedFile archivedFile)
        : this(archivedFile?.Document.Stream)
    { }

    public MarketDocumentStream(FileStorageFile fileStorageFile)
        : this(fileStorageFile?.Stream) { }

    public MarketDocumentStream(MarketDocumentWriterMemoryStream stream)
        : this((Stream)stream)
    { }

    private MarketDocumentStream(Stream? stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
    }

    public Stream Stream
    {
        get
        {
            _stream.Position = 0;
            return _stream;
        }
    }
}
