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
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.XPath;
using Messaging.Application.Xml;
using Xunit;

namespace Messaging.Tests.Infrastructure.OutgoingMessages.Asserts;

public class AssertXmlDocument
{
    private const string MarketActivityRecordElementName = "MktActivityRecord";
    private readonly Stream _stream;
    private readonly string _prefix;
    private readonly XDocument _document;
    private readonly XmlReader _reader;
    private readonly XmlNamespaceManager _xmlNamespaceManager;

    private AssertXmlDocument(Stream stream, string prefix)
    {
        _stream = stream;
        _prefix = prefix;
        _reader = XmlReader.Create(stream);
        _document = XDocument.Load(_reader);
        _xmlNamespaceManager = new XmlNamespaceManager(_reader.NameTable);
        _xmlNamespaceManager.AddNamespace(prefix, _document.Root!.Name.NamespaceName);
    }

    public static AssertXmlDocument Document(Stream document, string prefix)
    {
        return new AssertXmlDocument(document, prefix);
    }

    public AssertXmlDocument NumberOfMarketActivityRecordsIs(int expectedNumber)
    {
        Assert.Equal(expectedNumber, GetMarketActivityRecords().Count);
        return this;
    }

    public AssertXmlDocument NumberOfUsagePointLocationsIs(int expectedNumber)
    {
        Assert.Equal(expectedNumber, GetUsagePointLocations().Count);
        return this;
    }

    public AssertXmlDocument HasValue(string xpath, string expectedValue)
    {
        if (xpath == null) throw new ArgumentNullException(nameof(xpath));
        Assert.Equal(expectedValue, _document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager)?.Value);
        return this;
    }

    public AssertXmlDocument HasAttributeValue(string xpath, string attributeName, string expectedValue)
    {
        if (xpath == null) throw new ArgumentNullException(nameof(xpath));
        Assert.Equal(expectedValue, _document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager)?.Attribute(attributeName)?.Value);
        return this;
    }

    public AssertXmlDocument IsNotPresent(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        Assert.Null(_document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager));
        return this;
    }

    public async Task<AssertXmlDocument> HasValidStructureAsync(XmlSchema schema)
    {
        if (schema == null) throw new ArgumentNullException(nameof(schema));
        var validationResult = await MessageValidator.ValidateAsync(_stream, schema).ConfigureAwait(false);
        Assert.True(validationResult.IsValid);
        return this;
    }

    private List<XElement> GetMarketActivityRecords()
    {
        return _document.Root?.Elements()
            .Where(x => x.Name.LocalName.Equals(MarketActivityRecordElementName, StringComparison.OrdinalIgnoreCase))
            .ToList() ?? new List<XElement>();
    }

    private List<XElement> GetUsagePointLocations()
    {
        return _document.Root?.Descendants()
            .Where(x => x.Name.LocalName.Equals("UsagePointLocation", StringComparison.OrdinalIgnoreCase))
            .ToList() ?? new List<XElement>();
    }

    private string EnsureXPathHasPrefix(string xpath)
    {
        var elementNames = xpath.Split("/");
        var xpathBuilder = new StringBuilder();
        xpathBuilder.Append('.');
        foreach (var elementName in elementNames)
        {
            xpathBuilder.Append('/');
            if (!elementName.StartsWith(_prefix + ':', StringComparison.OrdinalIgnoreCase))
            {
                xpathBuilder.Append(_prefix + ":");
            }

            xpathBuilder.Append(elementName);
        }

        return xpathBuilder.ToString();
    }
}
