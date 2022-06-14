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
using Messaging.Application.Configuration;
using Messaging.Application.IncomingMessages;
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using NodaTime;

namespace Messaging.IntegrationTests.Application.IncomingMessages
{
    internal class IncomingMessageBuilder
    {
        private readonly string _createdAt = SystemClock.Instance.GetCurrentInstant().ToString();
        private readonly string _receiverRole = "DDQ";
        private readonly string _senderRole = "DDZ";
        private readonly Instant _effectiveDate = SystemClock.Instance.GetCurrentInstant();
        private readonly string _messageId = Guid.NewGuid().ToString();
        private string _processType = "NotSet";
        private string _senderId = "NotSet";
        private string _receiverId = DataHubDetails.IdentificationNumber;
        private string? _consumerName = "NotSet";
        private string _marketEvaluationPointId = "NotSet";

        internal IncomingMessageBuilder WithMarketEvaluationPointId(string marketEvaluationPointId)
        {
            _marketEvaluationPointId = marketEvaluationPointId;
            return this;
        }

        internal IncomingMessageBuilder WithProcessType(string processType)
        {
            _processType = processType;
            return this;
        }

        internal IncomingMessageBuilder WithSenderId(string senderId)
        {
            _senderId = senderId;
            return this;
        }

        internal IncomingMessageBuilder WithConsumerName(string? consumerName)
        {
            _consumerName = consumerName;
            return this;
        }

        internal IncomingMessageBuilder WithReceiver(string receiverId)
        {
            _receiverId = receiverId;
            return this;
        }

        internal IncomingMessage Build()
        {
            return IncomingMessage.Create(
                CreateHeader(),
                CreateMarketActivityRecord());
        }

        private MarketActivityRecord CreateMarketActivityRecord()
        {
            return new MarketActivityRecord()
            {
                BalanceResponsibleId = "fake",
                Id = Guid.NewGuid().ToString(),
                ConsumerId = "fake",
                ConsumerName = _consumerName,
                EffectiveDate = _effectiveDate.ToString(),
                EnergySupplierId = "fake",
                MarketEvaluationPointId = _marketEvaluationPointId,
            };
        }

        private MessageHeader CreateHeader()
        {
            return new MessageHeader(
                _messageId,
                _processType,
                _senderId,
                _senderRole,
                _receiverId,
                _receiverRole,
                _createdAt);
        }
    }
}
