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
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Files.Shares;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI;

namespace Energinet.DataHub.MarketRoles.Infrastructure.PostOffice
{
    public class TempPostOfficeStorageClient : IPostOfficeStorageClient
    {
        private readonly PostOfficeStorageClientSettings _settings;

        public TempPostOfficeStorageClient(
            PostOfficeStorageClientSettings settings)
        {
            _settings = settings;
        }

        public async Task WriteAsync(PostOfficeEnvelope message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var shareClient = new ShareClient(_settings.ConnectionString, _settings.ShareName);
            var traceContext = TraceContext.Parse(string.Empty); // TODO: Make use of message.Correlation when Telemetry is implemented
            var path = traceContext.IsValid ? traceContext.TraceId : "None";
            var shareDirectoryClient = shareClient.GetDirectoryClient(path);

            // Create the directory if it doesn't already exist
            await shareDirectoryClient.CreateIfNotExistsAsync().ConfigureAwait(false);

            // Ensure that the directory exists
            if (await shareDirectoryClient.ExistsAsync().ConfigureAwait(false))
            {
                var bytes = Encoding.ASCII.GetBytes(message.Content);

                var fileName = Guid.NewGuid().ToString() + ".json";
                var fileClient = await shareDirectoryClient.CreateFileAsync(fileName, bytes.LongLength).ConfigureAwait(false);
                await using var fileShareStream = await fileClient.Value.OpenWriteAsync(false, 0).ConfigureAwait(false);
                await using var localStream = new MemoryStream(bytes);
                await localStream.CopyToAsync(fileShareStream).ConfigureAwait(false);
            }
        }
    }

    public record PostOfficeStorageClientSettings(string ConnectionString, string ShareName);
}
