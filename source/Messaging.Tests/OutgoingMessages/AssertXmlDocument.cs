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
using Messaging.Application.Xml;
using Messaging.Domain.OutgoingMessages;
using Xunit;

namespace Messaging.Tests.OutgoingMessages;

public class AssertXmlDocument
{
    private const string MarketActivityRecordElementName = "MktActivityRecord";
    private readonly Stream _stream;
    private readonly XDocument _document;

    private AssertXmlDocument(Stream stream)
    {
        _stream = stream;
        _document = XDocument.Load(_stream);
    }

    public static AssertXmlDocument Document(Stream document)
    {
        return new AssertXmlDocument(document);
    }

    public AssertXmlDocument HasHeader(MessageHeader expectedHeader)
    {
        if (expectedHeader == null) throw new ArgumentNullException(nameof(expectedHeader));
        Assert.NotEmpty(GetMessageHeaderValue("mRID")!);
        Assert.Equal(expectedHeader.ProcessType, GetMessageHeaderValue("process.processType"));
        Assert.Equal("23", GetMessageHeaderValue("businessSector.type"));
        Assert.Equal(expectedHeader.SenderId, GetMessageHeaderValue("sender_MarketParticipant.mRID"));
        Assert.Equal(expectedHeader.SenderRole, GetMessageHeaderValue("sender_MarketParticipant.marketRole.type"));
        Assert.Equal(expectedHeader.ReceiverId, GetMessageHeaderValue("receiver_MarketParticipant.mRID"));
        Assert.Equal(expectedHeader.ReceiverRole, GetMessageHeaderValue("receiver_MarketParticipant.marketRole.type"));
        Assert.Equal(expectedHeader.ReasonCode, GetMessageHeaderValue("reason.code"));
        return this;
    }

    public AssertXmlDocument HasMarketActivityRecordValue(string marketActivityRecordId, string elementName, string? expectedValue)
    {
        var marketActivityRecord = GetMarketActivityRecordById(marketActivityRecordId);
        Assert.NotNull(marketActivityRecord);
        Assert.Equal(expectedValue, marketActivityRecord!.Element(marketActivityRecord.Name.Namespace + elementName)?.Value);
        return this;
    }

    public AssertXmlDocument NumberOfMarketActivityRecordsIs(int expectedNumber)
    {
        Assert.Equal(expectedNumber, GetMarketActivityRecords().Count);
        return this;
    }

    public AssertXmlDocument HasType(string expectedTypeCode)
    {
        Assert.Equal(expectedTypeCode, GetMessageHeaderValue("type"));
        return this;
    }

    public AssertMarketEvaluationPoint MarketEvaluationPoint(string marketActivityRecordId)
    {
        var marketActivityRecord = GetMarketActivityRecordById(marketActivityRecordId);
        var marketEvaluationPoint = marketActivityRecord!.Element(marketActivityRecord.Name.Namespace + "MarketEvaluationPoint");
        return new AssertMarketEvaluationPoint(marketEvaluationPoint!);
    }

    public async Task<AssertXmlDocument> HasValidStructureAsync(XmlSchema schema)
    {
        if (schema == null) throw new ArgumentNullException(nameof(schema));
        var validationResult = await MessageValidator.ValidateAsync(_stream, schema).ConfigureAwait(false);
        Assert.True(validationResult.IsValid);
        return this;
    }

    internal XElement? GetMarketActivityRecordById(string id)
    {
        var header = _document.Root!;
        var ns = header.Name.Namespace;
        return header
            .Elements(ns + MarketActivityRecordElementName)
            .FirstOrDefault(x => x.Element(ns + "mRID")?.Value
                .Equals(id, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private string? GetMessageHeaderValue(string elementName)
    {
        var header = GetHeaderElement();
        return header?.Element(header.Name.Namespace + elementName)?.Value;
    }

    private XElement? GetHeaderElement()
    {
        return _document.Root;
    }

    private List<XElement> GetMarketActivityRecords()
    {
        return _document.Root?.Elements()
            .Where(x => x.Name.LocalName.Equals(MarketActivityRecordElementName, StringComparison.OrdinalIgnoreCase))
            .ToList() ?? new List<XElement>();
    }
}

#pragma warning disable
public class AssertMarketEvaluationPoint
{
    private readonly XElement _marketEvaluationPointElement;

    public AssertMarketEvaluationPoint(XElement marketEvaluationPointElement)
    {
        _marketEvaluationPointElement = marketEvaluationPointElement;
    }

    public AssertMarketEvaluationPoint HasValue(string elementName, string expectedValue)
    {
        Assert.Equal(expectedValue, _marketEvaluationPointElement.Element(_marketEvaluationPointElement.Name.Namespace + elementName)?.Value);
        return this;
    }

    public AssertMarketEvaluationPoint NumberOfUsagePointLocationsIs(int expectedNumber)
    {
        Assert.Equal(expectedNumber, GetUsagePointLocations().Count);
        return this;
    }

    private List<XElement> GetUsagePointLocations()
    {
        return _marketEvaluationPointElement.Elements()
            .Where(x => x.Name.LocalName.Equals("UsagePointLocation", StringComparison.OrdinalIgnoreCase))
            .ToList() ?? new List<XElement>();
    }

    public AssertUsagePointLocation UsagePointLocation(int index)
    {
        return new AssertUsagePointLocation(GetUsagePointLocations()[index]);
    }
}

public class AssertUsagePointLocation
{
    private readonly XElement _usagePointLocationElement;

    public AssertUsagePointLocation(XElement usagePointLocationElement)
    {
        _usagePointLocationElement = usagePointLocationElement;
    }

    public AssertUsagePointLocation HasValue(string elementName, string expectedValue)
    {
        Assert.Equal(expectedValue, _usagePointLocationElement.Element(_usagePointLocationElement.Name.Namespace + elementName)?.Value);
        return this;
    }
}
