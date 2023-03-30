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
using System.Text.Json;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.SeedWork;

namespace Infrastructure.OutgoingMessages.Common.Json;

internal static class JsonHeaderWriter
{
    internal static void Write(MessageHeader messageHeader, string documentType, string typeCode, string? reasonCode, Utf8JsonWriter writer)
    {
        if (messageHeader == null) throw new ArgumentNullException(nameof(messageHeader));
        if (documentType == null) throw new ArgumentNullException(nameof(documentType));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        writer.WriteStartObject();
        writer.WritePropertyName(documentType);
        writer.WriteStartObject();

        writer.WriteProperty("mRID", messageHeader.MessageId);
        writer.WriteObject("businessSector.type", new KeyValuePair<string, string>("value", "23"));
        writer.WriteProperty("createdDateTime", messageHeader.TimeStamp.ToString());
        writer.WriteObject("process.processType", new KeyValuePair<string, string>("value", CimCode.Of(ProcessType.From(messageHeader.ProcessType))));

        if (reasonCode is not null)
        {
            writer.WriteObject("reason.code", new KeyValuePair<string, string>("value", reasonCode));
        }

        writer.WriteObject(
            "receiver_MarketParticipant.mRID",
            new KeyValuePair<string, string>("codingScheme", CimCode.CodingSchemeOf(ActorNumber.Create(messageHeader.ReceiverId))),
            new KeyValuePair<string, string>("value", messageHeader.ReceiverId));

        writer.WriteObject(
            "receiver_MarketParticipant.marketRole.type",
            new KeyValuePair<string, string>("value", CimCode.Of(EnumerationType.FromName<MarketRole>(messageHeader.ReceiverRole))));

        writer.WriteObject(
            "sender_MarketParticipant.mRID",
            new KeyValuePair<string, string>("codingScheme", CimCode.CodingSchemeOf(ActorNumber.Create(messageHeader.SenderId))),
            new KeyValuePair<string, string>("value", messageHeader.SenderId));

        writer.WriteObject(
            "sender_MarketParticipant.marketRole.type",
            new KeyValuePair<string, string>("value", CimCode.Of(EnumerationType.FromName<MarketRole>(messageHeader.SenderRole))));

        writer.WriteObject("type", new KeyValuePair<string, string>("value", typeCode));
    }
}
