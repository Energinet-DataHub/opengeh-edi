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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using Messaging.Application.OutgoingMessages.RejectRequestChangeOfSupplier;
using Messaging.Application.Xml;
using Messaging.Domain.OutgoingMessages;
using Xunit;

namespace Messaging.Tests.Infrastructure.OutgoingMessages.Asserts
{
    internal static class AssertXmlMessage
    {
        private const string MarketActivityRecordElementName = "MktActivityRecord";

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
            AssertHasHeaderValue(document, "process.processType", header.ProcessType);
            AssertHasHeaderValue(document, "businessSector.type", "23");
            AssertHasHeaderValue(document, "sender_MarketParticipant.mRID", header.SenderId);
            AssertHasHeaderValue(document, "sender_MarketParticipant.marketRole.type", header.SenderRole);
            AssertHasHeaderValue(document, "receiver_MarketParticipant.mRID", header.ReceiverId);
            AssertHasHeaderValue(document, "receiver_MarketParticipant.marketRole.type", header.ReceiverRole);
            AssertHasHeaderValue(document, "reason.code", header.ReasonCode);
        }

        internal static async Task AssertConformsToSchemaAsync(Stream message, XmlSchema schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            var validationResult = await MessageValidator.ValidateAsync(message, schema).ConfigureAwait(false);
            Assert.True(validationResult.IsValid);
        }

        internal static void AssertReasons(XElement marketActivityRecord, IReadOnlyList<Reason> expectedReasons)
        {
            var reasonsElements = marketActivityRecord.Elements(marketActivityRecord.Name.Namespace + "Reason").ToList();
            Assert.Equal(expectedReasons.Count, reasonsElements.Count);
            for (int i = 0; i < expectedReasons.Count; i++)
            {
                var actualCode = reasonsElements[i].Element(marketActivityRecord.Name.Namespace + "code")?.Value;
                var actualText = reasonsElements[i].Element(marketActivityRecord.Name.Namespace + "text")?.Value;
                Assert.Equal(expectedReasons[i].Code, actualCode);
                Assert.Equal(expectedReasons[i].Text, actualText);
            }
        }

        private static string? GetMessageHeaderValue(XDocument document, string elementName)
        {
            var header = GetHeaderElement(document);
            return header?.Element(header.Name.Namespace + elementName)?.Value;
        }

        private static XElement? GetHeaderElement(XDocument document)
        {
            return document.Root;
        }
    }
}
