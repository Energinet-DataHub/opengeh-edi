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

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Messaging.Bundling;
using Energinet.DataHub.MessageHub.Client.Storage;
using Energinet.DataHub.MessageHub.Model.Dequeue;
using Energinet.DataHub.MessageHub.Model.Peek;
using Processing.Domain.SeedWork;
using Processing.Infrastructure.LocalMessageHub;

namespace Energinet.DataHub.MarketRoles.Messaging
{
    public class LocalMessageHubClient : ILocalMessageHubClient
    {
        private readonly IOutboxDispatcher<MessageHubMessage> _messageHubMessageOutboxDispatcher;
        private readonly IOutboxDispatcher<DataBundleResponse> _dataBundleResponseOutboxDispatcher;
        private readonly IDequeueNotificationParser _dequeueNotificationParser;
        private readonly IRequestBundleParser _requestBundleParser;
        private readonly IBundleCreator _bundleCreator;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly IStorageHandler _storageHandler;
        private readonly IMessageHubMessageRepository _messageHubMessageRepository;

        public LocalMessageHubClient(
            IStorageHandler storageHandler,
            IMessageHubMessageRepository messageHubMessageRepository,
            IOutboxDispatcher<MessageHubMessage> messageHubMessageOutboxDispatcher,
            IOutboxDispatcher<DataBundleResponse> dataBundleResponseOutboxDispatcher,
            IDequeueNotificationParser dequeueNotificationParser,
            IRequestBundleParser requestBundleParser,
            IBundleCreator bundleCreator,
            ISystemDateTimeProvider systemDateTimeProvider)
        {
            _storageHandler = storageHandler;
            _messageHubMessageRepository = messageHubMessageRepository;
            _messageHubMessageOutboxDispatcher = messageHubMessageOutboxDispatcher;
            _dataBundleResponseOutboxDispatcher = dataBundleResponseOutboxDispatcher;
            _dequeueNotificationParser = dequeueNotificationParser;
            _requestBundleParser = requestBundleParser;
            _bundleCreator = bundleCreator;
            _systemDateTimeProvider = systemDateTimeProvider;
        }

        [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Issue: https://github.com/dotnet/roslyn-analyzers/issues/5712")]
        public async Task CreateBundleAsync(byte[] request, string sessionId)
        {
            var bundleRequestDto = _requestBundleParser.Parse(request);

            var dataAvailableIds = await _storageHandler.GetDataAvailableNotificationIdsAsync(bundleRequestDto).ConfigureAwait(false);
            var messages = await _messageHubMessageRepository.GetMessagesAsync(dataAvailableIds.ToArray()).ConfigureAwait(false);

            var bundle = await _bundleCreator.CreateBundleAsync(messages).ConfigureAwait(false);

            foreach (var message in messages)
            {
                message.AddToBundle(bundleRequestDto.IdempotencyId);
            }

            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(bundle));

            var uri = await _storageHandler.AddStreamToStorageAsync(stream, bundleRequestDto).ConfigureAwait(false);

            _dataBundleResponseOutboxDispatcher.Dispatch(new DataBundleResponse(bundleRequestDto, uri, sessionId));
        }

        public async Task BundleDequeuedAsync(byte[] notification)
        {
            var dequeueNotificationDto = _dequeueNotificationParser.Parse(notification);

            var dataAvailableIds = await _storageHandler.GetDataAvailableNotificationIdsAsync(dequeueNotificationDto).ConfigureAwait(false);
            var messages = await _messageHubMessageRepository.GetMessagesAsync(dataAvailableIds.ToArray()).ConfigureAwait(false);

            foreach (var message in messages)
            {
                message.Dequeue(_systemDateTimeProvider.Now());
                _messageHubMessageOutboxDispatcher.Dispatch(message);
            }
        }
    }
}
