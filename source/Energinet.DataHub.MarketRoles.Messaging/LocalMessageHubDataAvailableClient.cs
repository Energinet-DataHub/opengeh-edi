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
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI;
using Energinet.DataHub.MarketRoles.Infrastructure.LocalMessageHub;
using Energinet.DataHub.MessageHub.Model.Model;

namespace Energinet.DataHub.MarketRoles.Messaging
{
    public class LocalMessageHubDataAvailableClient : ILocalMessageHubDataAvailableClient
    {
        private readonly IOutboxDispatcher<DataAvailableNotification> _dataAvailableOutboxDispatcher;
        private readonly MessageHubMessageFactory _messageHubMessageFactory;
        private readonly ICorrelationContext _correlationContext;
        private readonly IMessageHubMessageRepository _messageHubMessageRepository;

        public LocalMessageHubDataAvailableClient(
            IMessageHubMessageRepository messageHubMessageRepository,
            IOutboxDispatcher<DataAvailableNotification> dataAvailableOutboxDispatcher,
            MessageHubMessageFactory messageHubMessageFactory,
            ICorrelationContext correlationContext)
        {
            _messageHubMessageRepository = messageHubMessageRepository;
            _dataAvailableOutboxDispatcher = dataAvailableOutboxDispatcher;
            _messageHubMessageFactory = messageHubMessageFactory;
            _correlationContext = correlationContext;
        }

        public void DataAvailable(MessageHubEnvelope messageHubEnvelope)
        {
            if (messageHubEnvelope is null)
            {
                throw new ArgumentNullException(nameof(messageHubEnvelope));
            }

            var traceContext = TraceContext.Parse(messageHubEnvelope.Correlation);
            _correlationContext.SetId(traceContext.TraceId);
            _correlationContext.SetParentId(traceContext.ParentId);

            var messageMetadata = _messageHubMessageFactory.Create(messageHubEnvelope.Correlation, messageHubEnvelope.Content, messageHubEnvelope.MessageType, messageHubEnvelope.Recipient, messageHubEnvelope.GsrnNumber);
            _messageHubMessageRepository.AddMessageMetadata(messageMetadata);

            _dataAvailableOutboxDispatcher.Dispatch(new DataAvailableNotification(messageMetadata.Id, new GlobalLocationNumberDto(messageHubEnvelope.Recipient), new MessageTypeDto(messageHubEnvelope.MessageType.Name), DomainOrigin.MeteringPoints, true, 1));
        }
    }
}
