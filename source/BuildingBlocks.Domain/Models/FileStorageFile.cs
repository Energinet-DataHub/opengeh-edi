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
public sealed record FileStorageFile : IDisposable
{
    private string? _contentAsString;
    private Stream? _contentAsStream;

    public FileStorageFile(Stream stream)
    {
        _contentAsStream = stream;
    }

    public FileStorageFile(string content)
    {
        _contentAsString = content;
    }

    /// <summary>
    /// Reads the content as a stream.
    /// </summary>
    public Stream ReadAsStream()
    {
        if (_contentAsStream != null)
            return _contentAsStream;

        if (_contentAsString is null)
            throw new NullReferenceException("Content and stream is null, cannot read from it");

        var memoryStream = new MemoryStream();
        using var streamWriter = new StreamWriter(memoryStream);
        streamWriter.Write(_contentAsString);
        streamWriter.Flush();
        memoryStream.Position = 0;

        _contentAsStream = memoryStream;

        return _contentAsStream;
    }

    /// <summary>
    /// Reads (and caches) the underlying stream into a string
    /// </summary>
    public async ValueTask<string> ReadAsStringAsync()
    {
        if (!string.IsNullOrEmpty(_contentAsString))
            return _contentAsString;

        if (_contentAsStream is null)
            throw new NullReferenceException("Stream and content is null, cannot read from it");

        if (_contentAsStream.Position != 0 && !_contentAsStream.CanSeek)
            throw new InvalidOperationException("Stream is already read from, and cannot perform seek to reset position");

        _contentAsStream.Position = 0;

        using var streamReader = new StreamReader(_contentAsStream);
        _contentAsString = await streamReader.ReadToEndAsync().ConfigureAwait(false);

        await _contentAsStream.DisposeAsync().ConfigureAwait(false);
        _contentAsStream = null;

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
            _contentAsString = null; // "Dispose" string by setting it to null
            _contentAsStream?.Dispose();
            _contentAsStream = null;
        }
    }
}
