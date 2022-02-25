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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MessageHub.Client.Storage;
using Energinet.DataHub.MessageHub.Model.Model;

namespace Energinet.DataHub.MarketRoles.Tests.LocalMessageHub.Mocks
{
    public class StorageHandlerMock : IStorageHandler
    {
        private readonly MessageHubMessageRepositoryMock _messageHubMessageRepository;

        public StorageHandlerMock(MessageHubMessageRepositoryMock messageHubMessageRepository)
        {
            _messageHubMessageRepository = messageHubMessageRepository;
        }

        public static Task<Stream> GetStreamFromStorageAsync(Uri contentPath)
        {
            if (contentPath == null) throw new ArgumentNullException(nameof(contentPath));
            return Task.FromResult((Stream)new MemoryStream());
        }

        public Task<Uri> AddStreamToStorageAsync(Stream stream, DataBundleRequestDto requestDto)
        {
            return Task.FromResult(new Uri("https://someUri"));
        }

        public Task<IReadOnlyList<Guid>> GetDataAvailableNotificationIdsAsync(DataBundleRequestDto bundleRequest)
        {
            return Task.FromResult((IReadOnlyList<Guid>)_messageHubMessageRepository.MessageHubMessages.Select(x => x.Id).ToList());
        }

        public Task<IReadOnlyList<Guid>> GetDataAvailableNotificationIdsAsync(DequeueNotificationDto dequeueNotification)
        {
            return Task.FromResult((IReadOnlyList<Guid>)_messageHubMessageRepository.MessageHubMessages.Select(x => x.Id).ToList());
        }
    }
}
