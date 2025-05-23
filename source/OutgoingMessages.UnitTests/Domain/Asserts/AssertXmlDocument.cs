﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.DocumentValidation;
using FluentAssertions;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Asserts;

public class AssertXmlDocument
{
    private readonly Stream _stream;
    private readonly string _prefix;
    private readonly DocumentValidator? _documentValidator;
    private readonly XDocument _document;
    private readonly XmlNamespaceManager _xmlNamespaceManager;

    private AssertXmlDocument(Stream stream, string prefix)
    {
        _stream = stream;
        _prefix = prefix;
        using var reader = XmlReader.Create(stream);
        _document = XDocument.Load(reader);
        _xmlNamespaceManager = new XmlNamespaceManager(reader.NameTable);
        _xmlNamespaceManager.AddNamespace(prefix, _document.Root!.Name.NamespaceName);
    }

    private AssertXmlDocument(Stream stream, string prefix, DocumentValidator documentValidator)
        : this(stream, prefix)
    {
        _documentValidator = documentValidator;
    }

    public XmlNamespaceManager XmlNamespaceManager => _xmlNamespaceManager;

    public static AssertXmlDocument Document(Stream document, string prefix, DocumentValidator validator)
    {
        return new AssertXmlDocument(document, prefix, validator);
    }

    public AssertXmlDocument HasValue(string xpath, string expectedValue)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        var pathSelectElement = _document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager);
        Assert.Equal(expectedValue, pathSelectElement?.Value);
        return this;
    }

    public AssertXmlDocument ElementExists(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        var pathSelectElement = _document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager);
        Assert.NotNull(pathSelectElement);
        return this;
    }

    public AssertXmlDocument HasAttribute(string xpath, string attribute, string expectedValue)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        (_document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager)
                ?.Attribute(attribute)
                ?.Value)
            .Should()
            .Be(expectedValue);

        return this;
    }

    public AssertXmlDocument IsNotPresent(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        Assert.Null(_document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager));
        return this;
    }

    public async Task<AssertXmlDocument> HasValidStructureAsync(DocumentType type, string version = "0.1")
    {
        ArgumentNullException.ThrowIfNull(_documentValidator);
        var validationResult = await _documentValidator.ValidateAsync(_stream, DocumentFormat.Xml, type, CancellationToken.None, version).ConfigureAwait(false);
        validationResult.ValidationErrors.Should().BeEmpty();
        Assert.True(validationResult.IsValid);
        return this;
    }

    public IList<XElement>? GetElements(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        return _document.Root?.XPathSelectElements(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager)
            .ToList();
    }

    public XElement? GetElement(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        return _document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager);
    }

    public string EnsureXPathHasPrefix(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);

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
