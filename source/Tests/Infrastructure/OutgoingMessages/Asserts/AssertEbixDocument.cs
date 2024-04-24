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
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;

public class AssertEbixDocument
{
    private readonly Stream _stream;
    private readonly string _prefix;
    private readonly DocumentValidator? _documentValidator;
    private readonly XDocument _document;
    private readonly XmlNamespaceManager _xmlNamespaceManager;
    private readonly XDocument _originalMessage;

    private AssertEbixDocument(Stream stream, string prefix)
    {
        _prefix = prefix;
        using var reader = XmlReader.Create(stream);

        _originalMessage = XDocument.Load(reader);

        var elm = _originalMessage.Root!.Descendants().Single(x => x.Name.LocalName == "Payload").Descendants().First();
        _document = XDocument.Parse(elm.ToString());

        _xmlNamespaceManager = new XmlNamespaceManager(reader.NameTable);
        _xmlNamespaceManager.AddNamespace(prefix, _document!.Root!.Name.NamespaceName);
        stream = new MemoryStream();
        _document.Save(stream);
        stream.Position = 0;
        _stream = stream;
    }

    private AssertEbixDocument(Stream stream, string prefix, DocumentValidator documentValidator)
        : this(stream, prefix)
    {
        _documentValidator = documentValidator;
    }

    public XmlNamespaceManager XmlNamespaceManager => _xmlNamespaceManager;

    public static AssertEbixDocument Document(Stream document, string prefix)
    {
        return new AssertEbixDocument(document, prefix);
    }

    public static AssertEbixDocument Document(Stream document, string prefix, DocumentValidator validator)
    {
        return new AssertEbixDocument(document, prefix, validator);
    }

    public AssertEbixDocument HasValue(string xpath, string expectedValue)
    {
        return HasValueWithAttributes(xpath, expectedValue);
    }

    public AssertEbixDocument ElementExists(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);

        Assert.NotNull(_document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager));

        return this;
    }

    public AssertEbixDocument HasValueWithAttributes(string xpath, string expectedValue, params AttributeNameAndValue[] attributes)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        ArgumentNullException.ThrowIfNull(attributes);
        Assert.Equal(expectedValue, _document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager)?.Value);

        foreach (var (name, value) in attributes)
            Assert.Equal(value, _document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager)?.Attribute(name)?.Value);

        return this;
    }

    public AssertEbixDocument IsNotPresent(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        Assert.Null(_document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager));

        return this;
    }

    public async Task<AssertEbixDocument> HasValidStructureAsync(DocumentType type, string version = "0.1", bool skipIdentificationLengthValidation = false)
    {
        Assert.True(_originalMessage.Root!.Name == "MessageContainer");
        Assert.NotNull(_originalMessage.Root!.Elements().Single(x => x.Name.LocalName == "MessageReference"));
        Assert.NotNull(_originalMessage.Root!.Elements().Single(x => x.Name.LocalName == "DocumentType"));
        Assert.NotNull(_originalMessage.Root!.Elements().Single(x => x.Name.LocalName == "MessageType"));
        var validationResult = await _documentValidator!.ValidateAsync(_stream, DocumentFormat.Ebix, type, CancellationToken.None, version).ConfigureAwait(false);

        if (!validationResult.IsValid && skipIdentificationLengthValidation)
        {
            var ignoreMaxLengthErrorsFor = new List<string>()
            {
                "NotifyAggregatedWholesaleServices:v3:Identification",
                "NotifyAggregatedWholesaleServices:v3:OriginalBusinessDocument",
                "RejectAggregatedBillingInformation:v3:Identification",
                "RejectAggregatedBillingInformation:v3:OriginalBusinessDocument",
            };

            var validationErrorsExceptId = validationResult.ValidationErrors
                .Where(errorMessage =>
                {
                    var isMaxLengthError = errorMessage.Contains(
                        "The actual length is greater than the MaxLength value",
                        StringComparison.OrdinalIgnoreCase);

                    var isIgnoredElement = ignoreMaxLengthErrorsFor.Any(name =>
                        errorMessage.Contains($"{name}' element is invalid", StringComparison.InvariantCulture));

                    var ignoreError = isMaxLengthError && isIgnoredElement;
                    return !ignoreError;
                })
                .ToArray();

            validationResult = ValidationResult.Invalid(validationErrorsExceptId);
        }

        validationResult.ValidationErrors.Should().BeEmpty();
        validationResult.IsValid.Should().BeTrue();

        return this;
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

    public IList<XElement>? GetElements(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        return _document.Root?.XPathSelectElements(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager).ToList();
    }

    public XElement? GetElement(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        return _document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager);
    }
}

public record AttributeNameAndValue(string Name, string Value);
