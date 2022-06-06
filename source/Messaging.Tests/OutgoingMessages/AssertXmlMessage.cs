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
using System.Xml.Linq;
using Messaging.Application.OutgoingMessages;
using Xunit;

namespace Messaging.Tests.OutgoingMessages
{
    internal static class AssertXmlMessage
    {
        private const string MarketActivityRecordElementName = "MktActivityRecord";

        internal static string? GetMessageHeaderValue(XDocument document, string elementName)
        {
            var header = GetHeaderElement(document);
            return header?.Element(header.Name.Namespace + elementName)?.Value;
        }

        internal static XElement? GetMarketActivityRecordById(XDocument document, string id)
        {
            var header = document.Root!;
            var ns = header.Name.Namespace;
            return header
                .Elements(ns + MarketActivityRecordElementName)
                .FirstOrDefault(x => x.Element(ns + "mRID")?.Value
                    .Equals(id, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        internal static List<XElement> GetMarketActivityRecords(XDocument document)
        {
            return document.Root?.Elements()
                .Where(x => x.Name.LocalName.Equals(MarketActivityRecordElementName, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<XElement>();
        }

        internal static void AssertHasHeaderValue(XDocument document, string elementName, string? expectedValue)
        {
            Assert.Equal(expectedValue, GetMessageHeaderValue(document, elementName));
        }

        internal static void AssertMarketActivityRecordValue(XElement marketActivityRecord, string elementName, string? expectedValue)
        {
            Assert.Equal(expectedValue, marketActivityRecord.Element(marketActivityRecord.Name.Namespace + elementName)?.Value);
        }

        internal static void AssertMarketActivityRecordCount(XDocument document, int expectedCount)
        {
            Assert.Equal(expectedCount, GetMarketActivityRecords(document).Count);
        }

        internal static void AssertHeader(MessageHeader header, XDocument document)
        {
            Assert.NotEmpty(AssertXmlMessage.GetMessageHeaderValue(document, "mRID")!);
            AssertXmlMessage.AssertHasHeaderValue(document, "type", "414");
            AssertXmlMessage.AssertHasHeaderValue(document, "process.processType", header.ProcessType);
            AssertXmlMessage.AssertHasHeaderValue(document, "businessSector.type", "23");
            AssertXmlMessage.AssertHasHeaderValue(document, "sender_MarketParticipant.mRID", header.SenderId);
            AssertXmlMessage.AssertHasHeaderValue(document, "sender_MarketParticipant.marketRole.type", header.SenderRole);
            AssertXmlMessage.AssertHasHeaderValue(document, "receiver_MarketParticipant.mRID", header.ReceiverId);
            AssertXmlMessage.AssertHasHeaderValue(document, "receiver_MarketParticipant.marketRole.type", header.ReceiverRole);
            AssertXmlMessage.AssertHasHeaderValue(document, "reason.code", header.ReasonCode);
        }

        private static XElement? GetHeaderElement(XDocument document)
        {
            return document.Root;
        }
    }
}
