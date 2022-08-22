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
using System.Diagnostics;
using System.Threading.Tasks;
using Messaging.Domain.OutgoingMessages;
using Newtonsoft.Json;

namespace Messaging.Application.Common;

public static class JsonHeaderWriter
{
    public static void Write(MessageHeader messageHeader, string documentType, string typeCode, JsonTextWriter writer)
    {
        if (messageHeader == null) throw new ArgumentNullException(nameof(messageHeader));
        if (documentType == null) throw new ArgumentNullException(nameof(documentType));
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        writer.Formatting = Formatting.Indented;

        writer.WriteStartObject();
        writer.WritePropertyName(documentType);
        writer.WriteStartObject();
        writer.WritePropertyName("mRID");
        writer.WriteValue(messageHeader.MessageId);

        writer.WritePropertyName("businessSector.type");
        writer.WriteStartObject();
        writer.WritePropertyName("value");
        writer.WriteValue("23");
        writer.WriteEndObject();

        writer.WritePropertyName("createdDateTime");
        writer.WriteValue(messageHeader.TimeStamp.ToString());

        writer.WritePropertyName("process.processType");
        writer.WriteStartObject();
        writer.WritePropertyName("value");
        writer.WriteValue(messageHeader.ProcessType);
        writer.WriteEndObject();

        writer.WritePropertyName("reason.code");
        writer.WriteStartObject();
        writer.WritePropertyName("value");
        writer.WriteValue(messageHeader.ReasonCode);
        writer.WriteEndObject();

        writer.WritePropertyName("receiver_MarketParticipant.mRID");
        writer.WriteStartObject();
        writer.WritePropertyName("codingScheme");
        writer.WriteValue("A10");
        writer.WritePropertyName("value");
        writer.WriteValue(messageHeader.ReceiverId);
        writer.WriteEndObject();

        writer.WritePropertyName("receiver_MarketParticipant.marketRole.type");
        writer.WriteStartObject();
        writer.WritePropertyName("value");
        writer.WriteValue(messageHeader.ReceiverRole);
        writer.WriteEndObject();

        writer.WritePropertyName("sender_MarketParticipant.mRID");
        writer.WriteStartObject();
        writer.WritePropertyName("codingScheme");
        writer.WriteValue("A10");
        writer.WritePropertyName("value");
        writer.WriteValue(messageHeader.SenderId);
        writer.WriteEndObject();

        writer.WritePropertyName("sender_MarketParticipant.marketRole.type");
        writer.WriteStartObject();
        writer.WritePropertyName("value");
        writer.WriteValue(messageHeader.SenderRole);
        writer.WriteEndObject();

        writer.WritePropertyName("type");
        writer.WriteStartObject();
        writer.WritePropertyName("value");
        writer.WriteValue(typeCode);
        writer.WriteEndObject();
    }
}
