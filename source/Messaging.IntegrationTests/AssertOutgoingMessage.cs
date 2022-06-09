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
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Newtonsoft.Json.Linq;
using NodaTime;
using Xunit;

namespace Messaging.IntegrationTests
{
    public class AssertOutgoingMessage
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly dynamic _message;

        private AssertOutgoingMessage(dynamic message, IDbConnectionFactory connectionFactory)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Assert.NotNull(message);
            _message = message;
            _connectionFactory = connectionFactory;
        }

        public static AssertOutgoingMessage OutgoingMessage(string transactionId, string documentType, string processType, IDbConnectionFactory connectionFactory)
        {
            if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));

            var messages = connectionFactory.GetOpenConnection().QuerySingle(
                $"SELECT m.Id, m.RecordId, m.DocumentType, m.RecipientId, m.CorrelationId, m.OriginalMessageId, m.ProcessType," +
                $"m.ReceiverRole, m.SenderId, m.SenderRole, m.ReasonCode, m.MarketActivityRecordPayload " +
                $" FROM [b2b].[OutgoingMessages] m" +
                $" WHERE m.OriginalMessageId = '{transactionId}' AND m.DocumentType = '{documentType}' AND m.ProcessType = '{processType}'");

            Assert.NotNull(messages);
            return new AssertOutgoingMessage(messages, connectionFactory);
        }

        public AssertOutgoingMessage HasReceiverId(string receiverId)
        {
            Assert.Equal(receiverId, _message.RecipientId);
            return this;
        }

        public AssertOutgoingMessage HasReceiverRole(string receiverRole)
        {
            Assert.Equal(receiverRole, _message.ReceiverRole);
            return this;
        }

        public AssertOutgoingMessage HasSenderId(string senderId)
        {
            Assert.Equal(senderId, _message.SenderId);
            return this;
        }

        public AssertOutgoingMessage HasSenderRole(string senderRole)
        {
            Assert.Equal(senderRole, _message.SenderRole);
            return this;
        }

        public AssertOutgoingMessage HasReasonCode(string? reasonCode)
        {
            Assert.Equal(reasonCode, _message.ReasonCode);
            return this;
        }

        public AssertMarketActivityRecord WithMarketActivityRecord()
        {
            return new AssertMarketActivityRecord(_message.MarketActivityRecordPayload);
        }
    }

    #pragma warning disable
    public class AssertMarketActivityRecord
    {
        private readonly JToken _payload;

        public AssertMarketActivityRecord(string payload)
        {
            _payload = JToken.Parse(payload);
        }

        public AssertMarketActivityRecord HasId()
        {
            Assert.NotNull(_payload.Value<string>("Id"));
            return this;
        }

        public AssertMarketActivityRecord HasOriginalTransactionId(string originalTransactionId)
        {
            Assert.Equal(originalTransactionId, _payload.Value<string>("OriginalTransactionId"));
            return this;
        }

        public AssertMarketActivityRecord HasMarketEvaluationPointId(string marketEvaluationPointId)
        {
            Assert.Equal(marketEvaluationPointId, _payload.Value<string>("MarketEvaluationPointId"));
            return this;
        }

        public AssertMarketActivityRecord HasValidityStart(DateTime validityStart)
        {
            Assert.Equal(validityStart, _payload.Value<DateTime>("ValidityStart"));
            return this;
        }
    }
}
