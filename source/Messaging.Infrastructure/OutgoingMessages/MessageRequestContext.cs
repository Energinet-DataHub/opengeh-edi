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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MessageHub.Client.Storage;
using Energinet.DataHub.MessageHub.Model.Model;
using Energinet.DataHub.MessageHub.Model.Peek;

namespace Messaging.Infrastructure.OutgoingMessages
{
    public class MessageRequestContext
    {
        private readonly IRequestBundleParser _requestBundleParser;
        private readonly IStorageHandler _storageHandler;

        public MessageRequestContext(
            IRequestBundleParser requestBundleParser,
            IStorageHandler storageHandler)
        {
            _requestBundleParser = requestBundleParser;
            _storageHandler = storageHandler;
        }

        public DataBundleRequestDto? DataBundleRequestDto { get; private set; }

        public IReadOnlyCollection<string>? DataAvailableIds { get; private set; }

        public async Task SetMessageRequestContextAsync(byte[] data)
        {
            DataBundleRequestDto = _requestBundleParser.Parse(data);
            var dataAvailableIds = await _storageHandler.GetDataAvailableNotificationIdsAsync(DataBundleRequestDto)
                .ConfigureAwait(false);
            DataAvailableIds = dataAvailableIds.Select(x => x.ToString()).ToList();
        }
    }
}
