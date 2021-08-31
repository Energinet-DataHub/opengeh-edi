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
using System.Linq;
using Google.Protobuf;

namespace Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf
{
    public class ProtobufMessageFactory : IProtobufMessageFactory
    {
        private readonly Lazy<Dictionary<string, MessageParser>> _messageParserMaps = new(() =>
            ProtobufMessageParserMap.GetKnownParsers().ToDictionary(key => key.EventName, val => val.Parser));

        public IMessage CreateMessageFrom(byte[] payload, string messageTypeName)
        {
            if (!_messageParserMaps.Value.ContainsKey(messageTypeName))
            {
                throw new InvalidOperationException($"Could not locate parser for message type {messageTypeName}");
            }

            return _messageParserMaps.Value[messageTypeName].ParseFrom(payload);
        }
    }
}
