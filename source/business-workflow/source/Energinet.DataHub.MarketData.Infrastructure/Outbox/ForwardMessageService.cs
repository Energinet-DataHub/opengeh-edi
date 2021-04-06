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
using Energinet.DataHub.PostOffice.Contracts;
using Google.Protobuf;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.MarketData.Infrastructure.Outbox
{
    public class ForwardMessageService : IForwardMessageService
    {
        private readonly IForwardMessageRepository _forwardMessageRepository;
        private readonly IPostOfficeService _postOfficeService;

        public ForwardMessageService(
            IForwardMessageRepository forwardMessageRepository,
            IPostOfficeService postOfficeService)
        {
            _forwardMessageRepository = forwardMessageRepository;
            _postOfficeService = postOfficeService;
        }

        public async Task ProcessMessagesAsync()
        {
            // Fetch the first message to process.
            var message = await _forwardMessageRepository.GetUnprocessedForwardMessageAsync().ConfigureAwait(false);

            // Keep iterating as long as we have a message.
            while (message != null)
            {
                var postOfficeDocument = MapForwardMessageToPostOfficeDocument(message).ToByteArray();

                // TODO: Implement some way of ordering and grouping messages. We need metering point created messages to arrive at the suppliers before the metering point info messages. See https://dev.azure.com/energinet/Datahub/_boards/board/t/Batman/Stories/?workitem=119557 for more info
                await _postOfficeService.SendMessageAsync(postOfficeDocument);
                await _forwardMessageRepository.MarkForwardedMessageAsProcessedAsync(message.Id).ConfigureAwait(false);

                // Fetch the next message.
                message = await _forwardMessageRepository.GetUnprocessedForwardMessageAsync().ConfigureAwait(false);
            }
        }

        private static Document MapForwardMessageToPostOfficeDocument(ForwardMessage message)
        {
            return new Document
            {
                Content = message.Data,
                Recipient = message.Recipient,
                Type = message.Type,
                Version = "v1",
                EffectuationDate = message.OccurredOn.ToTimestamp(),
            };
        }
    }
}
