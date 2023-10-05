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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Energinet.DataHub.EDI.Infrastructure.DocumentValidation;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;

public class AssertEbixDocument
{
    private readonly string _prefix;
    private readonly XDocument _document;
    private readonly XmlNamespaceManager _xmlNamespaceManager;

    private AssertEbixDocument(Stream stream, string prefix)
    {
        _prefix = prefix;
        using var reader = XmlReader.Create(stream);
        _document = XDocument.Load(reader);
        _xmlNamespaceManager = new XmlNamespaceManager(reader.NameTable);
        _xmlNamespaceManager.AddNamespace(prefix, _document.Root!.Name.NamespaceName);
    }

    public static AssertEbixDocument Document(Stream document, string prefix)
    {
        return new AssertEbixDocument(document, prefix);
    }

    public AssertEbixDocument HasValue(string xpath, string expectedValue)
    {
        if (xpath == null) throw new ArgumentNullException(nameof(xpath));
        Assert.Equal(expectedValue, _document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager)?.Value);
        return this;
    }

    public AssertEbixDocument IsNotPresent(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        Assert.Null(_document.Root?.XPathSelectElement(EnsureXPathHasPrefix(xpath), _xmlNamespaceManager));
        return this;
    }

    public async Task<AssertEbixDocument> HasValidStructureAsync(DocumentType type, string version = "0.1")
    {
        //TODO: Schema validation will be implemented later
        await Task.CompletedTask.ConfigureAwait(true);
        Assert.True(true);
        return this;
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
