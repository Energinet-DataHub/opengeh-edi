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

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.BaseParsers.Xml;

internal static class XmlReaderExtensions
{
    public static bool Is(this XmlReader reader, string localName, string ns, XmlNodeType xmlNodeType = XmlNodeType.Element)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(localName);
        ArgumentNullException.ThrowIfNull(ns);

        return reader.LocalName.Equals(localName, StringComparison.Ordinal) &&
               reader.NamespaceURI.Equals(ns, StringComparison.Ordinal) &&
               reader.NodeType == xmlNodeType;
    }

    public static async ValueTask<RootElement> ReadRootElementAsync(this XmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        do
        {
            if (reader.NodeType != XmlNodeType.Element) continue;

            return new RootElement(reader.LocalName, reader.NamespaceURI);
        }
        while (await reader.ReadAsync().ConfigureAwait(false));

        throw new InvalidOperationException("Reached end of xml without finding the root element!");
    }

    public static async ValueTask<XmlReader> AdvanceToAsync(this XmlReader reader, string localName, string ns)
    {
        do
        {
            if (reader.LocalName == localName &&
                reader.NamespaceURI == ns &&
                reader.NodeType == XmlNodeType.Element)
            {
                return reader;
            }
        }
        while (await reader.ReadAsync().ConfigureAwait(false));

        throw new XmlException("Xml node not found");
    }

    public static async ValueTask ReadToEndAsync(this XmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        while (await reader.ReadAsync().ConfigureAwait(false))
        { }
    }
}
