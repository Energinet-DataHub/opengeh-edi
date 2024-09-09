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

using System.Xml.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Assertions;

internal sealed class AssertXmlMessage
{
    private const string MarketActivityRecordElementName = "MktActivityRecord";
    private const string SerieRecordElementName = "Series";
    private readonly XDocument _document;

    private AssertXmlMessage(XDocument document)
    {
        _document = document;
    }

    internal static void IsTheSameDocument(Stream document1, Stream document2)
    {
        Assert.Equal(
            GetMessageHeaderValue(XDocument.Load(document1), "mRID"),
            GetMessageHeaderValue(XDocument.Load(document2), "mRID"));
    }

    internal static AssertXmlMessage Document(XDocument document)
    {
        return new AssertXmlMessage(document);
    }

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

    internal static List<XElement> GetSerieRecords(XDocument document)
    {
        return document.Root?.Elements()
            .Where(x => x.Name.LocalName.Equals(SerieRecordElementName, StringComparison.OrdinalIgnoreCase))
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

    internal AssertXmlMessage HasMarketActivityRecordCount(int expectedCount)
    {
        Assert.Equal(expectedCount, GetMarketActivityRecords(_document).Count);
        return this;
    }

    internal AssertXmlMessage HasSerieRecordCount(int expectedCount)
    {
        Assert.Equal(expectedCount, GetSerieRecords(_document).Count);
        return this;
    }

    internal AssertXmlMessage IsDocumentType(DocumentType documentType)
    {
        Assert.Equal(documentType.Name + "_MarketDocument", _document.Root!.Name.LocalName);
        return this;
    }

    internal AssertXmlMessage IsBusinessReason(BusinessReason businessReason)
    {
        var messageHeaderValue = GetMessageHeaderValue("process.processType");
        Assert.Equal(businessReason.Code, messageHeaderValue);
        return this;
    }

    internal AssertXmlMessage HasReceiver(ActorNumber actorNumber)
    {
        Assert.Equal(actorNumber.Value, GetMessageHeaderValue("receiver_MarketParticipant.mRID"));
        return this;
    }

    internal AssertXmlMessage HasReceiverRole(ActorRole actorRole)
    {
        Assert.Equal(actorRole.Code, GetMessageHeaderValue("receiver_MarketParticipant.marketRole.type"));
        return this;
    }

    internal string? GetMessageHeaderValue(string elementName)
    {
        var header = GetHeaderElement(_document);
        return header?.Element(header.Name.Namespace + elementName)?.Value;
    }

    private static XElement? GetHeaderElement(XDocument document)
    {
        return document.Root;
    }
}
