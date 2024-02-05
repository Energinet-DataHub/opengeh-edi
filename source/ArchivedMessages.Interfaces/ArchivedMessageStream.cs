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

namespace Energinet.DataHub.EDI.ArchivedMessages.Interfaces;

#pragma warning disable CA1711 // Is a "Stream" value type
public sealed record ArchivedMessageStream : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream;

    public ArchivedMessageStream(FileStorageFile fileStorageFile)
    {
        ArgumentNullException.ThrowIfNull(fileStorageFile);

        _stream = fileStorageFile.Stream;
    }

    public Stream Stream
    {
        get
        {
            _stream.Position = 0;
            return _stream;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
            _stream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
