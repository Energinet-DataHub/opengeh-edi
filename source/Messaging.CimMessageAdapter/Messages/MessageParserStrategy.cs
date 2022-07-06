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
using System.Net.Mime;

namespace Messaging.CimMessageAdapter.Messages;

public class MessageParserStrategy
{
    private readonly XmlMessageParser _xmlMessageParser;
    private readonly JsonMessageParser _jsonMessageParser;

    private readonly IDictionary<string, MessageParser> _strategies;

    public MessageParserStrategy(XmlMessageParser xmlMessageParser, JsonMessageParser jsonMessageParser)
    {
        _xmlMessageParser = xmlMessageParser;
        _jsonMessageParser = jsonMessageParser;

        _strategies = new Dictionary<string, MessageParser>()
        {
            { MediaTypeNames.Application.Xml, _xmlMessageParser },
            { MediaTypeNames.Application.Json, _jsonMessageParser },
        };
    }

    public MessageParser GetMessageParser(string contentType)
    {
        var strategy = _strategies.FirstOrDefault(s => string.Equals(s.Key, contentType, StringComparison.OrdinalIgnoreCase));
        if (strategy.Key is null) throw new InvalidOperationException($"No message parser strategy found for content type {contentType}");
        return strategy.Value;
    }
}
