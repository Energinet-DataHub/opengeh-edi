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

namespace Messaging.CimMessageAdapter.Messages;

public class MessageParser
{
    private readonly XmlMessageParserStrategy _xmlMessageParserStrategy;
    private readonly JsonMessageParserStrategy _jsonMessageParserStrategy;

    private readonly IDictionary<string, MessageParserStrategy> _strategies;

    public MessageParser(XmlMessageParserStrategy xmlMessageParserStrategy, JsonMessageParserStrategy jsonMessageParserStrategy)
    {
        _xmlMessageParserStrategy = xmlMessageParserStrategy;
        _jsonMessageParserStrategy = jsonMessageParserStrategy;

        _strategies = new Dictionary<string, MessageParserStrategy>()
        {
            { CimFormat.Xml.Name, _xmlMessageParserStrategy },
            { CimFormat.Json.Name, _jsonMessageParserStrategy },
        };
    }

    public MessageParserStrategy GetMessageParserStrategy(string cimFormat)
    {
        var strategy = _strategies.FirstOrDefault(s => string.Equals(s.Key, cimFormat, StringComparison.OrdinalIgnoreCase));
        if (strategy.Key is null) throw new InvalidOperationException($"No message parser strategy found for content type {cimFormat}");
        return strategy.Value;
    }
}
