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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

/// <summary>
/// A file downloaded from File Storage, using a IFileStorageClient/>
/// </summary>
public sealed record FileStorageFile(Stream Stream) : IDisposable
{
    private string? _contentAsString;

    /// <summary>
    /// The <see cref="Stream"/> contains the stream from file storage. If using a DataLakeFileStorageClient the file is downloaded the first time this stream is read.
    /// </summary>
    public Stream Stream { get; } = Stream;

    /// <summary>
    /// Reads and caches the underlying stream into a string
    /// </summary>
    public async Task<string> ReadAsStringAsync()
    {
        if (!string.IsNullOrEmpty(_contentAsString))
            return _contentAsString;

        var stream = Stream;
        if (stream.Position != 0 && !stream.CanSeek)
            throw new InvalidOperationException("Stream is already read from, and cannot perform seek to reset position");

        stream.Position = 0;

#pragma warning disable CA2000 // We cannot dispose the stream reader, disposing a stream reader disposes the underlying stream, which means we can't read it again
        var streamReader = new StreamReader(stream);
#pragma warning restore CA2000
        _contentAsString = await streamReader.ReadToEndAsync().ConfigureAwait(false);

        return _contentAsString;
    }

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stream.Dispose();
        }
    }
}
