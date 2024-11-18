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

using System.Xml;
using System.Xml.Linq;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.BaseParsers.Xml;

internal static class MessageHeaderExtractor
{
    private const string MridElementName = "mRID";
    private const string TypeElementName = "type";
    private const string ProcessTypeElementName = "process.processType";
    private const string SenderMridElementName = "sender_MarketParticipant.mRID";
    private const string SenderRoleElementName = "sender_MarketParticipant.marketRole.type";
    private const string ReceiverMridElementName = "receiver_MarketParticipant.mRID";
    private const string ReceiverRoleElementName = "receiver_MarketParticipant.marketRole.type";
    private const string CreatedDateTimeElementName = "createdDateTime";
    private const string BusinessSectorTypeElementName = "businessSector.type";

    public static async Task<MessageHeader> ExtractAsync(
        XmlReader reader,
        RootElement rootElement,
        string headerElementName,
        string marketActivityRecordElementName)
    {
        var messageId = string.Empty;
        var messageType = string.Empty;
        var processType = string.Empty;
        var senderId = string.Empty;
        var senderRole = string.Empty;
        var receiverId = string.Empty;
        var receiverRole = string.Empty;
        var createdAt = string.Empty;
        string? businessType = null;
        var ns = rootElement.DefaultNamespace;

        await reader.AdvanceToAsync(headerElementName, rootElement.DefaultNamespace).ConfigureAwait(false);

        while (!reader.EOF)
        {
            if (reader.Is(MridElementName, ns))
                messageId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is(TypeElementName, ns))
                messageType = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is(ProcessTypeElementName, ns))
                processType = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is(SenderMridElementName, ns))
                senderId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is(SenderRoleElementName, ns))
                senderRole = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is(ReceiverMridElementName, ns))
                receiverId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is(ReceiverRoleElementName, ns))
                receiverRole = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is(CreatedDateTimeElementName, ns))
                createdAt = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is(BusinessSectorTypeElementName, ns))
                businessType = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else await reader.ReadAsync().ConfigureAwait(false);

            if (reader.Is(marketActivityRecordElementName, ns)) break;
        }

        return new MessageHeader(messageId, messageType, processType, senderId, senderRole, receiverId, receiverRole, createdAt, businessType);
    }

    public static MessageHeader Extract(
        XDocument document,
        string headerElementName,
        XNamespace ns)
    {
        var headerElement = document.Descendants(ns + headerElementName).SingleOrDefault();
        if (headerElement == null) throw new InvalidOperationException("Header element not found");

        var messageId = headerElement.Element(ns + MridElementName)?.Value ?? string.Empty;
        var messageType = headerElement.Element(ns + TypeElementName)?.Value ?? string.Empty;
        var processType = headerElement.Element(ns + ProcessTypeElementName)?.Value ?? string.Empty;
        var senderId = headerElement.Element(ns + SenderMridElementName)?.Value ?? string.Empty;
        var senderRole = headerElement.Element(ns + SenderRoleElementName)?.Value ?? string.Empty;
        var receiverId = headerElement.Element(ns + ReceiverMridElementName)?.Value ?? string.Empty;
        var receiverRole = headerElement.Element(ns + ReceiverRoleElementName)?.Value ?? string.Empty;
        var createdAt = headerElement.Element(ns + CreatedDateTimeElementName)?.Value ?? string.Empty;
        var businessType = headerElement.Element(ns + BusinessSectorTypeElementName)?.Value;

        return new MessageHeader(
            messageId,
            messageType,
            processType,
            senderId,
            senderRole,
            receiverId,
            receiverRole,
            createdAt,
            businessType);
    }
}
