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

namespace Messaging.CimMessageAdapter.Messages;

public class MessageParser
{
    private readonly XmlMessageParserStrategy _xmlMessageParserStrategy;
    private readonly JsonMessageParserStrategy _jsonMessageParserStrategy;
    private readonly IEnumerable<IMessageParser> _parsers;

    private readonly IDictionary<CimFormat, MessageParserStrategy> _strategies;

    public MessageParser(XmlMessageParserStrategy xmlMessageParserStrategy, JsonMessageParserStrategy jsonMessageParserStrategy, IEnumerable<IMessageParser> parsers)
    {
        _xmlMessageParserStrategy = xmlMessageParserStrategy;
        _jsonMessageParserStrategy = jsonMessageParserStrategy;
        _parsers = parsers;

        _strategies = new Dictionary<CimFormat, MessageParserStrategy>()
        {
            { CimFormat.Xml, _xmlMessageParserStrategy },
            { CimFormat.Json, _jsonMessageParserStrategy },
        };
    }

    public Task<MessageParserResult> ParseAsync(Stream message, CimFormat cimFormat)
    {
        var parser = _parsers.FirstOrDefault(parser => parser.HandledFormat.Equals(cimFormat));
        if (parser is null) throw new InvalidOperationException($"No message parser found for message format '{cimFormat}'");
        return parser.ParseAsync(message);
    }
}
