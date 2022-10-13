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

using System.Threading.Tasks;
using System.Xml;
using Messaging.Application.IncomingMessages;

namespace Messaging.Infrastructure.IncomingMessages;

internal static class MessageHeaderExtractor
{
    public static async Task<MessageHeader> ExtractAsync(
        XmlReader reader,
        RootElement rootElement,
        string headerElementName,
        string marketActivityRecordElementName)
    {
        var messageId = string.Empty;
        var processType = string.Empty;
        var senderId = string.Empty;
        var senderRole = string.Empty;
        var receiverId = string.Empty;
        var receiverRole = string.Empty;
        var createdAt = string.Empty;
        var ns = rootElement.DefaultNamespace;

        await reader.AdvanceToAsync(headerElementName, rootElement.DefaultNamespace).ConfigureAwait(false);

        while (!reader.EOF)
        {
            if (reader.Is("mRID", ns))
                messageId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is("process.processType", ns))
                processType = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is("sender_MarketParticipant.mRID", ns))
                senderId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is("sender_MarketParticipant.marketRole.type", ns))
                senderRole = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is("receiver_MarketParticipant.mRID", ns))
                receiverId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is("receiver_MarketParticipant.marketRole.type", ns))
                receiverRole = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is("createdDateTime", ns))
                createdAt = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else await reader.ReadAsync().ConfigureAwait(false);

            if (reader.Is(marketActivityRecordElementName, ns)) break;
        }

        return new MessageHeader(messageId, processType, senderId, senderRole, receiverId, receiverRole, createdAt);
    }
}
